using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MovieRecommender.Models;

namespace MovieRecommender.Controllers.Api
{
    public class Movies : ApiController
    {

        // GET api/Movies
        [Route("api/Movies/{title}")]
        public IHttpActionResult GetMovies([FromUri] string title)
        {
           Movie movie=new Movie();
            return Ok(movie);
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