﻿/* Copyright 2016 Lars Blockken

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
using System.Net;
using Newtonsoft.Json;
using System.IO;

namespace SugarTools
{
    [Serializable]
    class SugarRestException : WebException
    {
        public SugarRestException(WebException e, SugarRest sugarRest, string call, string method, object data)
        {
            if (!ReferenceEquals(e.Response, null))
            {
                using (StreamReader sr = new StreamReader(e.Response.GetResponseStream()))
                {
                    dynamic result = JsonConvert.DeserializeObject(sr.ReadToEnd());

                    if (result.error.Equals("invalid_grant") && result.error_message.Equals("The access token provided is invalid."))
                    {
                        sugarRest.refresh();
                        sugarRest.call(call, method, data);
                    }
                    else if (result.error.Equals("invalid_grant") && result.error_message.Equals("Invalid refresh token"))
                    {
                        throw new WebException(result.error_message, e);
                    }
                    else
                    {
                        throw new WebException(result.error_message, e);
                    }
                }
            } else
            {
                throw new WebException(e.Message,e);
            }
        }
        public SugarRestException() : base() {}
        public SugarRestException(string Message) : base(Message) {}
    }
}