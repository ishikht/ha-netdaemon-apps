﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace System.Net.Http.Json.local
{
    internal static class JsonHelpers
    {
        internal static readonly JsonSerializerOptions s_defaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        internal static MediaTypeHeaderValue GetDefaultMediaType() => new("application/json") { CharSet = "utf-8" };

        internal static Encoding? GetEncoding(string? charset)
        {
            Encoding? encoding = null;

            if (charset != null)
            {
                try
                {
                    // Remove at most a single set of quotes.
                    if (charset.Length > 2 && charset[0] == '\"' && charset[charset.Length - 1] == '\"')
                    {
                        encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                    }
                    else
                    {
                        encoding = Encoding.GetEncoding(charset);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new InvalidOperationException("SR.CharSetInvalid", e);
                }

                Debug.Assert(encoding != null);
            }

            return encoding;
        }
    }
}
