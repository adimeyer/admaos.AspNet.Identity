using System;
using Raven.Imports.Newtonsoft.Json;
using Raven.Imports.Newtonsoft.Json.Linq;

namespace admaos.AspNet.Identity.RavenDB
{
    // http://stackoverflow.com/questions/27311635/how-do-i-parse-a-json-string-to-a-c-sharp-object-using-inheritance-polymorphis/27313288#27313288

    /// <summary>
    /// Generic converter class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObjectsWithMultipleConstructorsJsonConverter<T> : JsonConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        protected abstract T Create(JObject jObject);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            // Create target object based on JObject
            T target = Create(jObject);

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }
    }
}