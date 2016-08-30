﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MovieRecommender
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //Enable camel casing for json
            var settings = config.Formatters.JsonFormatter.SerializerSettings;
            settings.ContractResolver=new CamelCasePropertyNamesContractResolver();
            settings.Formatting=Formatting.Indented;
        }
    }
}
