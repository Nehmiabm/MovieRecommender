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

                    DbDataReader ratingReader = cmd.ExecuteReader();

                    while (ratingReader.Read())
                    {
                        Movie m = new Movie();
                        m.Genre = ratingReader["genre"].ToString();
                        m.MovieId = Convert.ToInt32(ratingReader["movieid"]);
                        m.Title = ratingReader["title"].ToString();
                        m.Year = Convert.ToInt32(ratingReader["year"]);

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
                    cmd.CommandText = "SELECT * FROM movie where title LIKE '%"+title+"%';";

                    DbDataReader ratingReader = cmd.ExecuteReader();

                    while (ratingReader.Read())
                    {
                        Movie m = new Movie();
                        m.Genre = ratingReader["genre"].ToString();
                        m.MovieId = Convert.ToInt32(ratingReader["movieid"]);
                        m.Title = ratingReader["title"].ToString();
                        m.Year = Convert.ToInt32(ratingReader["year"]);

                        movies.Add(m);
                    }
                }
            }
           
            return movies;
        }

        // GET api/Movies/recommendations/{userId}
        [Route("recommendations/{userId}")]
        public IHttpActionResult GetRecommendations([FromUri] int userId)
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

                Console.WriteLine("...........................................");
                int userID = 0;
                int loop = 0;
                List<object[]> templist = new List<object[]>();
                while (ratingReader.Read())
                {
                    object[] uval = new object[3];
                    uval[0] = ratingReader.GetInt32(0); //user
                    uval[1] = ratingReader["movieid"]; //movieid
                    uval[2] = ratingReader.GetInt32(2); //rating


                    if (userID != ratingReader.GetInt32(0) && loop++ != 0)
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

                        preferences.Put(userID, usePref);
                        templist = new List<object[]>();
                    }
                    else
                    {
                        templist.Add(uval);
                    }

                    userID = ratingReader.GetInt32(0);
                    // Console.WriteLine(userReader.GetInt32(0) + ".........  " + userReader.GetString(1));
                }

                if (templist.Count > 0)
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

                    preferences.Put(userID, usePref);
                }

                model = new GenericDataModel(preferences);


                //IDataModel model = await GetMovieDataModel();
                Console.WriteLine("Building model done!");
                Console.WriteLine("Calculating Recommendation...");
                //Creating UserSimilarity object.
                IUserSimilarity usersimilarity = new LogLikelihoodSimilarity(model);

                //Creating UserNeighbourHHood object.
                IUserNeighborhood userneighborhood = new NearestNUserNeighborhood(15, usersimilarity, model);

                //Create UserRecomender
                IUserBasedRecommender recommender = new GenericUserBasedRecommender(model, userneighborhood, usersimilarity);

                var recommendations = recommender.Recommend(userId, 10);

                foreach (IRecommendedItem recommendation in recommendations)
                {
                    Console.WriteLine(recommendation);
                }
                var recMovieIds = recommendations.Select(n => n.GetItemID()).ToList();

                OdbcCommand movieCommand = conn.CreateCommand();
                movieCommand.CommandText = "SELECT * FROM movie where movieId in ("+ string.Join(",", recMovieIds)+");";

                DbDataReader movieReader = movieCommand.ExecuteReader();

                Console.WriteLine("...........................................");

                while (movieReader.Read())
                {
                    Movie m=new Movie();

                    m.MovieId = Convert.ToInt32(movieReader["movieid"]);
                    m.Title = movieReader["title"].ToString(); //title
                    m.Year = Convert.ToInt32(movieReader["year"]);
                    m.Genre = movieReader["genre"].ToString();

                    movies.Add(m);

                }



            }

            return Ok(movies);
        }

        // GET api/<controller>/5
        [Route("api/Movies/Rating")]
        [HttpPost]
        public IHttpActionResult PostRating(int userId)
        {
            Rating rate = new Rating();
            return Created(new Uri(Request.RequestUri + "/" + userId), rate);
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