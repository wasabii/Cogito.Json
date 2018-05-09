﻿using System;
using System.Collections.Generic;

using Cogito.Json.Schema.Reducers;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Cogito.Json.Schema
{

    /// <summary>
    /// Transformor implementation that applies a series of reductions to a <see cref="JSchema"/>.
    /// </summary>
    class JSchemaReducingTransformer :
        JSchemaTransformer
    {

        readonly IList<JSchemaReducer> reducers;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="reducers"></param>
        public JSchemaReducingTransformer(IList<JSchemaReducer> reducers)
        {
            this.reducers = reducers ?? throw new ArgumentNullException(nameof(reducers));
        }

        public override JSchema Transform(JSchema schema)
        {
            // nothing to possibly transform
            if (schema == null)
                return null;

            // depth first
            var s1 = base.Transform(schema);
            if (s1 == null)
                return null;

            // apply reductions
            for (int i = 0; i < reducers.Count; i++)
            {
                var r = reducers[i];
                if (r == null)
                    continue;

                // reduce current schema
                var s2 = r.Reduce(s1);

                // reference equality means absolutely no possibility of change
                if (!ReferenceEquals(s1, s2))
                {
                    // going to need this for comparison
                    var j1 = JObject.FromObject(s1);
                    var j2 = JObject.FromObject(s2);

                    if (!JTokenEquals(j1, j2))
                    {
                        // start over using changed
                        s1 = s2;
                        i = -1;
                    }
                }
            }

            // resulting schema has been fully reduced
            return s1;
        }

        /// <summary>
        ///  Returns <c>true</c> if the two <see cref="JSchema"/> objects are equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool JTokenEquals(JToken a, JToken b)
        {
            return Equals(a, b) || JToken.DeepEquals(a, b);
        }

    }

}
