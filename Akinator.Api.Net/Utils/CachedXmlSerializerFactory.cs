using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Akinator.Api.Net.Utils
{
    public static class CachedXmlSerializerFactory
    {
        private static readonly object MSyncRootCache = new object();
        private static readonly Dictionary<Type, XmlSerializer> MCache = new Dictionary<Type, XmlSerializer>();
        
        public static XmlSerializer Create(Type type)
        {
            XmlSerializer serializer;
            lock (MSyncRootCache)
            {
                if (MCache.TryGetValue(type, out serializer))
                {
                    return serializer;
                }
            }
            lock (type)
            {
                serializer = XmlSerializer.FromTypes(new[] { type })[0];
            }
            lock (MSyncRootCache)
            {
                MCache[type] = serializer;
            }

            return serializer;
        }
    }
}
