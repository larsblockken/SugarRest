/* Copyright 2016 Lars Blockken

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. */

using System;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using Newtonsoft.Json;

namespace SugarTools
{
    /// <summary>
    /// Wrapper class for SugarCRM(c) v10 REST API
    /// Only available for Sugar 7.x and upwards
    /// </summary>
    public class SugarRest
    {
        private string accessToken, refreshToken, clientId, clientSecret;
        private Uri URL;
        private CookieContainer cc = new CookieContainer();
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="endpoint">URL to sugar instance (eg. https://instance.sugarondemand.com)</param>
        public SugarRest(Uri endpoint)
        {
            URL = endpoint;
        }

        /// <summary>
        /// Constructor that also logs in as the specified user
        /// </summary>
        /// <param name="endpoint">URL to sugar instance (eg. https://instance.sugarondemand.com)</param>
        /// <param name="u">Username</param>
        /// <param name="p">Password</param>
        public SugarRest(Uri endpoint, string u, string p)
        {
            URL = endpoint;
            login(u, p);
        }

        /// <summary>
        /// Constructor allowing the user to login with specified Oauth tokens
        /// </summary>
        /// <param name="endpoint">URL to sugar instance (eg. https://instance.sugarondemand.com)</param>
        /// <param name="u">Username</param>
        /// <param name="p">Password</param>
        /// <param name="tokenId">Oauth Token ID created in the Sugar instance</param>
        /// <param name="tokenSecret">Oauth Token Secret created in the Sugar instance</param>
        public SugarRest(Uri endpoint, string u, string p, string tokenId, string tokenSecret)
        {
            URL = endpoint;
            login(u, p, tokenId, tokenSecret);
        }

        /// <summary>
        /// Generic function to perform REST call
        /// </summary>
        /// <param name="call">relative path to rest call (eg. /rest/v10/Accounts)</param>
        /// <param name="method">HTTP Verb (GET, POST, PUT, DELETE)</param>
        /// <param name="data">Data variable with body contents</param>
        /// <returns>Dynamic object with result</returns>
        public dynamic call(string call, string method, object data = null)
        {
            if (!call.Equals("/rest/v10/oauth2/token/") && string.IsNullOrEmpty(accessToken)) {
                throw new SugarRestException("You must authenticate first");
            }

            try
            {
                Uri requestUrl = new Uri(URL + call);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUrl);
                req.Method = method;
                req.ContentType = "application/json;charset=utf-8;";
                req.CookieContainer = cc;
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    req.Headers.Add("oauth-token", accessToken);
                }

                if ((method.Equals("POST") || method.Equals("PUT")) && !ReferenceEquals(null, data))
                {
                    string json = JsonConvert.SerializeObject(data);
                    UTF8Encoding encoding = new UTF8Encoding();
                    byte[] bytes = encoding.GetBytes(json);
                    req.ContentLength = bytes.Length;
                    using (Stream requestStream = req.GetRequestStream())
                    {
                        requestStream.Write(bytes, 0, bytes.Length);
                    }
                }

                using (WebResponse response = req.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        dynamic result = JsonConvert.DeserializeObject(sr.ReadToEnd());
                        return result;
                    }
                }
            }
            catch (WebException e)
            {
                throw new SugarRestException(e, this, call, method, data);
            }
        }

        /// <summary>
        /// Perform login action and retrieve Access and Refresh tokens
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="id">Oauth Token ID</param>
        /// <param name="secret">Oauth Token Secret</param>
        /// <param name="platform">Platform, default "base"</param>
        public void login(string username, string password, string id = "sugar", string secret = "", string platform="base")
        {
            clientId = id;
            clientSecret = secret;
            var data = new {
                grant_type = "password",
                client_id = clientId,
                client_secret = clientSecret,
                username = username,
                password = password,
                platform = platform
            };
            dynamic result = this.call("/rest/v10/oauth2/token/", "POST", data);
            accessToken = result.access_token;
            refreshToken = result.refresh_token;
        }

        /// <summary>
        /// Performs an authentication request with the refresh token
        /// </summary>
        public void refresh()
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var data = new {
                    grant_type = "refresh_token",
                    refresh_token = refreshToken,
                    client_id = clientId,
                    client_secret = clientSecret
                };
                dynamic result = this.call("/rest/v10/oauth2/token/", "POST", data);
                accessToken = result.access_token;
                refreshToken = result.refresh_token;
            } else
            {
                throw new SugarRestException("No refresh token found. Unable to perform a refresh");
            }
        }

        /// <summary>
        /// Gets the current user preferences
        /// </summary>
        /// <returns>Dynamic object with user preferences</returns>
        public dynamic me()
        {
            return this.call("/rest/v10/me","GET");
        }

        /// <summary>
        /// Perform global search action
        /// </summary>
        /// <param name="query">Query to search on</param>
        /// <param name="maxNum">Maximum amount of records to return</param>
        /// <param name="offset">Amount of records to skip. Default is 0</param>
        /// <param name="fields">Comma delimited list of fields to retrieve</param>
        /// <param name="orderBy">Comma delimited list of fields to sort on appended with a colon and the direction (eg. name:DESC,account_type:DESC,date_modified:ASC)</param>
        /// <param name="favorites">Option to only select from favorite records. Possible values 0,1</param>
        /// <param name="myItems">Option to only select from ittems assigned to the authenticated user. Possible values 0,1</param>
        /// <returns>Dynamic object with record_offset and records array</returns>
        public dynamic search(string query, int maxNum = 20, int offset = 0, string fields = null, string orderBy = null, int favorites = 0, int myItems = 0)
        {
            var url = new StringBuilder();
            url.Append("/rest/v10/search?q=" + HttpUtility.UrlEncode(query));
            url.Append("&max_num=" + maxNum);
            url.Append("&offset=" + offset);
            url.Append("&favorites=" + favorites);
            url.Append("&my_items=" + myItems);
            if (string.IsNullOrEmpty(fields))
                url.Append("&fields=" + HttpUtility.UrlEncode(fields));
            if (string.IsNullOrEmpty(orderBy))
                url.Append("&orderBy=" + HttpUtility.UrlEncode(orderBy));

            return this.call(url.ToString(), "GET");
        }

        /// <summary>
        /// Perform search action on the users module
        /// </summary>
        /// <param name="query">Query to search on</param>
        /// <param name="maxNum">Maximum amount of user records to return</param>
        /// <param name="offset">Amount of records to skip. Default is 0</param>
        /// <param name="fields">Comma delimited list of fields to retrieve</param>
        /// <param name="orderBy">Comma delimited list of fields to sort on appended with a colon and the direction (eg. first_name:DESC,last_name:ASC)</param>
        /// <param name="favorites">Option to only select from favorite records. Possible values 0,1</param>
        /// <param name="myItems">Option to only select from ittems assigned to the authenticated user. Possible values 0,1</param>
        /// <returns>Dynamic object with record_offset and records array of users</returns>
        public dynamic searchUsers(string query, int maxNum = 20, int offset = 0, string fields = null, string orderBy = null, int favorites = 0, int myItems = 0)
        {
            var url = new StringBuilder();
            url.Append("/rest/v10/Users?q=" + HttpUtility.UrlEncode(query));
            url.Append("&max_num=" + maxNum);
            url.Append("&offset=" + offset);
            url.Append("&favorites=" + favorites);
            url.Append("&my_items=" + myItems);
            if (!string.IsNullOrEmpty(fields))
                url.Append("&fields=" + HttpUtility.UrlEncode(fields));
            if (!string.IsNullOrEmpty(orderBy))
                url.Append("&orderBy=" + HttpUtility.UrlEncode(orderBy));

            return this.call(url.ToString(), "GET");
        }

        /// <summary>
        /// Perform search action on module
        /// </summary>
        /// <param name="module">Module name</param>
        /// <param name="query">Search query</param>
        /// <param name="maxNum">Maximum numbers to return</param>
        /// <param name="offset">Amount of records you would like to skip. Default is 0</param>
        /// <param name="fields">Comma delimited list of fields to retrieve</param>
        /// <param name="view">Instead of the fields parameter, the view argument can be used. The field list will be built on the server side based on the selected view. Common views are "record" and "list"</param>
        /// <param name="orderBy">Comma delimited list of fields to sort on appended with a colon and the direction (eg. name:DESC,account_type:DESC,date_modified:ASC)</param>
        /// <param name="deleted">Show deleted records</param>
        /// <returns>Dynamic object with record_offset and records array</returns>
        public dynamic searchModule(string module, string query, int maxNum = 20, int offset = 0, string fields = null, string view = null, string orderBy = null, int deleted = 0)
        {
            var url = new StringBuilder();
            url.Append("/rest/v10/" + HttpUtility.UrlEncode(module));
            url.Append("?q=" + query);
            url.Append("&max_num=" + maxNum);
            url.Append("&offset=" + offset);
            url.Append("&deleted=" + deleted);
            if (!string.IsNullOrEmpty(fields))
                url.Append("&fields=" + HttpUtility.UrlEncode(fields));
            if (!string.IsNullOrEmpty(fields))
                url.Append("&view=" + HttpUtility.UrlEncode(view));
            if (!string.IsNullOrEmpty(orderBy))
                url.Append("&orderBy=" + HttpUtility.UrlEncode(orderBy));

            return this.call(url.ToString(), "GET");
        }

        /// <summary>
        /// Log message to the SugarCRM log (Authenticated)
        /// </summary>
        /// <param name="message">String message</param>
        /// <param name="logLevel">Possible log levels: debug, info, warn, deprecated, error, fatal, security</param>
        public void logMessage(string message, string logLevel)
        {
            var data = new
            {
                level = logLevel,
                message = message
            };
            this.call("/rest/v10/logger", "POST", data);
        }

        /// <summary>
        /// Create a record in sugar
        /// </summary>
        /// <param name="module">Module name (eg. Accounts, Contacts, ...)</param>
        /// <param name="record">Record object as documented by SugarCRM
        /// eg.
        /// var record = new
        ///     {
        ///         name = "Example C# Account",
        ///         description = "test",
        ///         meetings = new
        ///         {
        ///             create = new[] { new { name = "test c# 3", date_start = "2016-03-18T10:00:00-00:00" }
        ///         },
        ///         contacts = new
        ///         {
        ///             add = new[] {"id1","id2","idn"}
        ///         }
        ///     }
        /// };
        /// </param>
        /// <returns>Object with Sugar response</returns>
        public dynamic createRecord(string module, object record)
        {
            return this.call("/rest/v10/"+HttpUtility.UrlEncode(module),"POST", record);
        }

        /// <summary>
        /// Retrieve specefic record
        /// </summary>
        /// <param name="module">Module name</param>
        /// <param name="id">Sugar ID of record to retrieve</param>
        /// <returns>Dynamic object with the account information</returns>
        public dynamic retrieveRecord(string module, string id)
        {
            string rawUrl = "/rest/v10/" + HttpUtility.UrlEncode(module) + "/" + HttpUtility.UrlEncode(id);
            return this.call(rawUrl, "GET");
        }

        /// <summary>
        /// Update record in SugarCRM
        /// </summary>
        /// <param name="module">Module name (eg. Accounts, Contacts, ...)</param>
        /// <param name="id">record id in Sugar</param>
        /// <param name="data">Record object as documented by SugarCRM
        /// eg.
        /// var record = new
        ///     {
        ///         name = "Example C# Account",
        ///         description = "test",
        ///         meetings = new
        ///         {
        ///             create = new[] { new { name = "test c# 3", date_start = "2016-03-18T10:00:00-00:00" }
        ///         },
        ///         contacts = new
        ///         {
        ///             add = new[] {"id1","id2","idn"},
        ///             delete = new[] {"id"}
        ///         }
        ///     }
        /// };
        /// </param>
        /// <returns>Object with Sugar response</returns>
        public dynamic updateRecord(string module, string id, object data)
        {
            return this.call("/rest/v10/" + HttpUtility.UrlEncode(module) + "/" + HttpUtility.UrlEncode(id), "PUT", data);
        }

        /// <summary>
        /// Delete record in Sugar
        /// </summary>
        /// <param name="module">string Module name</param>
        /// <param name="id">string Record ID</param>
        /// <returns>Dynamic Sugar response</returns>
        public dynamic deleteRecord(string module, string id)
        {
            return this.call("/rest/v10/" + HttpUtility.UrlEncode(module) + "/" + HttpUtility.UrlEncode(id), "DELETE");
        }

        /// <summary>
        /// Mark specified record as favorite for the authenticated user
        /// </summary>
        /// <param name="module">string Module name (eg. Accounts, Contacts, ...)</param>
        /// <param name="id">string Record ID</param>
        /// <returns>Dynamic SugarCRM response</returns>
        public dynamic setFavorite(string module, string id)
        {
            return this.call("/rest/v10/" + HttpUtility.UrlEncode(module) + "/" + HttpUtility.UrlEncode(id) + "/favorite","PUT");
        }

        /// <summary>
        /// Unmarks the specified record as a favorite for the authenticated user
        /// </summary>
        /// <param name="module">string Module name (eg. Accounts, Contacts, ...)</param>
        /// <param name="id">string Record ID</param>
        /// <returns>Dynamic SugarCRM response</returns>
        public dynamic unsetFavorite(string module, string id)
        {
            return this.call("/rest/v10/" + HttpUtility.UrlEncode(module) + "/" + HttpUtility.UrlEncode(id) + "/favorite", "DELETE");
        }

        /// <summary>
        /// Retrieve attached file
        /// </summary>
        /// <param name="module">Module name</param>
        /// <param name="recordId">Record id</param>
        /// <param name="fileField">File field name</param>
        /// <returns>Stream with file contents</returns>
        public Stream getFile(string module, string recordId, string fileField)
        {
            throw new NotImplementedException();
        }


    }
}
