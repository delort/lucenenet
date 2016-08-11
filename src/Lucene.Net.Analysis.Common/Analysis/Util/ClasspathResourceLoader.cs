﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Lucene.Net.Analysis.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Simple <seealso cref="ResourceLoader"/> that uses <seealso cref="ClassLoader#getResourceAsStream(String)"/>
    /// and <seealso cref="Class#forName(String,boolean,ClassLoader)"/> to open resources and
    /// classes, respectively.
    /// </summary>
    public sealed class ClasspathResourceLoader : IResourceLoader
    {
        // LUCENENET NOTE: This class was refactored significantly from its Java counterpart.

        private readonly Type clazz;
        private readonly string namespaceExcludeRegex;

        /// <summary>
        /// Creates an instance using the System.Assembly of the given class to load Resources and classes
        /// Resource paths must be absolute.
        /// </summary>
        public ClasspathResourceLoader(Type clazz)
        {
            this.clazz = clazz;
        }

        /// <summary>
        /// Creates an instance using the System.Assembly of the given class to load Resources and classes
        /// Resource names are relative to the resourcePrefix.
        /// </summary>
        /// <param name="clazz">The class type</param>
        /// <param name="namespacePrefixToExclude">Removes the part of the namespace of the class that matches the regex. 
        /// This is useful to get to the resource if the assembly name and namespace name don't happen to match.
        /// If provided, the assembly name will be concatnated with the namespace name (excluding the part tha matches the regex)
        /// to provide the complete path to the embedded resource in the assembly. Note you can view the entire path to all of 
        /// the resources by calling Assembly.GetManifestResourceNames() so you can better understand how to build this path.</param>
        public ClasspathResourceLoader(Type clazz, string namespaceExcludeRegex)
        {
            this.clazz = clazz;
            this.namespaceExcludeRegex = namespaceExcludeRegex;
        }

        public Stream OpenResource(string resource)
        {
            var qualifiedResourceName = this.GetQualifiedResourceName(resource);
            Stream stream = this.clazz.Assembly.GetManifestResourceStream(qualifiedResourceName);
            if (stream == null)
            {
                throw new IOException("Resource not found: " + qualifiedResourceName);
            }
            return stream;
        }

        public Type FindClass(string cname)
        {
            try
            {
                return this.clazz.Assembly.GetType(cname, true);
            }
            catch (Exception e)
            {
                throw new Exception("Cannot load class: " + cname, e);
            }
        }

        public T NewInstance<T>(string cname)
        {
            Type clazz = FindClass(cname);
            try
            {
                return (T)Activator.CreateInstance(clazz);
            }
            catch (Exception e)
            {
                throw new Exception("Cannot create instance: " + cname, e);
            }
        }

        /// <summary>
        /// LUCENENET: Added for .NET help in finding the resource name.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private string GetQualifiedResourceName(string resource)
        {
            var namespaceName = this.clazz.Namespace;
            var assemblyName = clazz.Assembly.GetName().Name;
            if (string.IsNullOrEmpty(this.namespaceExcludeRegex) && (assemblyName.Equals(namespaceName, StringComparison.OrdinalIgnoreCase)))
                return namespaceName;

            string namespaceSegment = "";
            if (!string.IsNullOrEmpty(this.namespaceExcludeRegex))
            {
                // Remove the part of the path that matches the Regex.
                namespaceSegment = Regex.Replace(namespaceName, this.namespaceExcludeRegex, string.Empty, RegexOptions.Compiled);
            }

            // Qualify by namespace and separate by .
            return string.Concat(assemblyName, ".", namespaceSegment.Trim('.'), ".", resource).Replace("..", ".");
        }
    }
}