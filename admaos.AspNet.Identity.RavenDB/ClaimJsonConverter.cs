﻿using System.Security.Claims;
using Raven.Imports.Newtonsoft.Json;
using Raven.Imports.Newtonsoft.Json.Linq;

namespace admaos.AspNet.Identity.RavenDB
{
    // http://stackoverflow.com/questions/39334304/what-is-the-recommended-way-to-deserialize-an-object-with-several-constructors
    // http://stackoverflow.com/questions/27311635/how-do-i-parse-a-json-string-to-a-c-sharp-object-using-inheritance-polymorphis/27313288#27313288

    /// <summary>
    /// Since System.Security.Claims.Claim has no default constructor and more than one non-default constructor Newtonsoft.Json is not able
    /// to deserialize the object and a custom JsonConverter is needed.
    /// </summary>
    public class ClaimJsonConverter : ObjectsWithMultipleConstructorsJsonConverter<Claim>
    {
        public ClaimJsonConverter()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        protected override Claim Create(JObject jObject)
        {
            return new Claim(jObject[nameof(Claim.Type)].ToString(), jObject[nameof(Claim.Value)].ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //We don't deal with writing json, generally newtonsoft would make a good job of
            //serializing these type of objects without having to use a custom writer anyway
        }
    }
}