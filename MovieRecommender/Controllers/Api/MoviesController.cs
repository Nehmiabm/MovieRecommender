using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MovieRecommender.Models;
using Newtonsoft.Json.Linq;
using NReco.CF.Taste.Impl.Common;
using NReco.CF.Taste.Impl.Model;
using NReco.CF.Taste.Impl.Neighborhood;
using NReco.CF.Taste.Impl.Recommender;
using NReco.CF.Taste.Impl.Similarity;
using NReco.CF.Taste.Model;
using NReco.CF.Taste.Neighborhood;
using NReco.CF.Taste.Recommender;
using NReco.CF.Taste.Similarity;

namespace MovieRecommender.Controllers.Api
{
    [RoutePrefix("api/Movies")]
    public class MoviesController : ApiController
    {
        // GET api/Movies
        [Route("")]
        public IEnumerable<Movie> GetAllMovies()
        {
            List<Movie> movies = new List<Movie>();
            using (OdbcConnection conn =
                new OdbcConnection(connectionString: "DSN=Sample Microsoft Hive DSN;UID=admin;PWD=Password@123"))
            {
                conn.OpenAsync().Wait();
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM movie;";

                    DbDataReader movieDataReader = cmd.ExecuteReader();

                    while (movieDataReader.Read())
                    {
                        Movie m = new Movie();
                        m.Genre = movieDataReader["genre"].ToString();
                        
                        m.MovieId = Int32.Parse(movieDataReader["movieid"].ToString());
                        m.Title = movieDataReader["title"].ToString();
                        m.Year = Int32.Parse(movieDataReader["year"].ToString());

                        movies.Add(m);
                    }
                }
            }

            return movies;
        }

        // GET api/Movies/{title}
        [Route("{title}")]
        public IEnumerable<Movie> GetMovies([FromUri] string title)
        {
            List<Movie> movies = new List<Movie>();
            using (OdbcConnection conn =
                new OdbcConnection(connectionString: "DSN=Sample Microsoft Hive DSN;UID=admin;PWD=Password@123"))
            {
                conn.OpenAsync().Wait();
                using (OdbcCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM movie where UPPER(title) LIKE '%" + title.ToUpper()+"%';";

                    DbDataReader movieDataReader = cmd.ExecuteReader();

                    while (movieDataReader.Read())
                    {
                        Movie m = new Movie();
                        m.Genre = movieDataReader["genre"].ToString();
                        m.MovieId = Int32.Parse(movieDataReader["movieid"].ToString());
                        m.Title = movieDataReader["title"].ToString();
                        m.Year = Int32.Parse(movieDataReader["year"].ToString());

                        movies.Add(m);
                    }
                }
            }
           
            return movies;
        }

        // GET api/Movies/recommendations/{userId}
        [Route("recommendations/{userIdPar}")]
        public IHttpActionResult GetRecommendations([FromUri] int userIdPar)
        {
            List<Movie> movies = new List<Movie>();
            IDataModel model = null;
            using (OdbcConnection conn =
                   new OdbcConnection(connectionString: "DSN=Sample Microsoft Hive DSN;UID=admin;PWD=Password@123"))
            {
                FastByIDMap<IPreferenceArray> preferences = new FastByIDMap<IPreferenceArray>();
                conn.OpenAsync().Wait();
                OdbcCommand ratingCommand = conn.CreateCommand();
                ratingCommand.CommandText = "SELECT * FROM rating;";

                DbDataReader ratingReader = ratingCommand.ExecuteReader();

                int userId = 0;
                int loop = 0;
                bool userexists = false;
                List<object[]> templist = new List<object[]>();
                while (ratingReader.Read())
                {
                    object[] uval = new object[3];
                    uval[0] = ratingReader.GetInt32(0); //user
                    uval[1] = ratingReader["movieid"]; //movieid
                    uval[2] = ratingReader.GetInt32(2); //rating

                    if (userId != ratingReader.GetInt32(0) && loop++ != 0)
                    {
                        IPreferenceArray usePref = new GenericUserPreferenceArray(templist.Count);
                        int j = 0;
                        foreach (var urate in templist)
                        {
                            if (j == 0)
                                usePref.SetUserID(0, Convert.ToInt32(urate[0]));
                            usePref.SetItemID(j, Convert.ToInt64(urate[1]));
                            usePref.SetValue(j, Convert.ToInt64(urate[2]));
                            j++;
                        }

                        preferences.Put(userId, usePref);
                        templist = new List<object[]>();
                    }
                    else
                    {
                        templist.Add(uval);
                    }

                    userId = ratingReader.GetInt32(0);
                    if (userId == userIdPar)
                    {
                        userexists = true;
                    }
                }

                if (templist.Any())
                {
                    IPreferenceArray usePref = new GenericUserPreferenceArray(templist.Count);
                    int k = 0;
                    foreach (var urate in templist)
                    {
                        if (k == 0)
                            usePref.SetUserID(0, Convert.ToInt32(urate[0]));
                        usePref.SetItemID(k, Convert.ToInt64(urate[1]));
                        usePref.SetValue(k, Convert.ToInt64(urate[2]));
                        k++;
                    }

                    preferences.Put(userId, usePref);
                }

                if (userexists == false)
                {
                    return NotFound();
                }

                //Building model done!
                model = new GenericDataModel(preferences);

                //Calculating Recommendation...
                //1.Creating UserSimilarity object.
                IUserSimilarity usersimilarity = new LogLikelihoodSimilarity(model);

                //2.Creating UserNeighborhood object.
                IUserNeighborhood userneighborhood = new NearestNUserNeighborhood(15, usersimilarity, model);

                //3.Create UserRecomender
                IUserBasedRecommender recommender = new GenericUserBasedRecommender(model, userneighborhood, usersimilarity);

                var recommendations = recommender.Recommend(userIdPar, 10);

                var recMovieIds = recommendations.Select(n => n.GetItemID()).ToList();

                if (!recMovieIds.Any())
                {
                    return NotFound();
                }

                OdbcCommand movieCommand = conn.CreateCommand();
                movieCommand.CommandText = "SELECT * FROM movie where movieId in ("+ string.Join(",", recMovieIds)+");";

                DbDataReader movieReader = movieCommand.ExecuteReader();

                while (movieReader.Read())
                {
                    Movie m=new Movie();

                    m.MovieId = Int32.Parse(movieReader["movieid"].ToString());
                    m.Title = movieReader["title"].ToString(); //title
                    m.Year = Int32.Parse(movieReader["year"].ToString());
                    m.Genre = movieReader["genre"].ToString();

                    movies.Add(m);

                }


            }

            return Ok(movies);
        }

        // GET api/Movies/Rating
        [Route("Rating")]
        [HttpPost]
        public IHttpActionResult PostRating(Rating rating)
        {
            if (rating == null)
            {
                return NotFound();
            }

            using (OdbcConnection conn =
    new OdbcConnection(connectionString: "DSN=Sample Microsoft Hive DSN;UID=admin;PWD=Password@123"))
            {
                conn.OpenAsync().Wait();
                OdbcCommand userRatedcmd = conn.CreateCommand();
                    userRatedcmd.CommandText = "SELECT * FROM rating where userId=" + rating.UserId +" and "+" movieid="+rating.MovieId+";";

                    DbDataReader userRatedReader = userRatedcmd.ExecuteReader();

                    if (!userRatedReader.Read())
                    {
                    OdbcCommand newuserRatecmd = conn.CreateCommand();
                    //userId,movieId,rating
                    newuserRatecmd.CommandText = "INSERT INTO rating VALUES ("+rating.UserId+","+rating.MovieId+","+rating.Preference+");";

                        int result = newuserRatecmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            return Created(new Uri(Request.RequestUri+""), rating);
                        }
                        else
                        {
                            return NotFound();

                        }
                    }
                    else
                    {
                            return NotFound();
                        
                    }
            }

        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}