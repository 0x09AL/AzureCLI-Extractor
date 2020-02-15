using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using CommandLine;
namespace AzureCLI_Extractor
{

    [Verb("adduser", HelpText = "Adds a Global Admin User")]
    class AddUserOptions
    {

        [Option('d', "displayname", Required = true, HelpText = "User display name.")]
        public string DisplayName { get; set; }


        [Option('u', "username", Required = true, HelpText = "Account username.")]
        public string Username { get; set; }

        [Option('a', "accountprincipal", Required = true, HelpText = "The account principal name. It should be something like user@company.onmicrosoft.com / user@company.com .")]
        public string UserPrincipal { get; set; }


        [Option('p', "password", Required = true, HelpText = "Account password.")]
        public string Password { get; set; }
    }


    [Verb("gettoken", HelpText = "Gets the user access token.")]
    class TokenOptions
    {

        [Option('v', "verbose", Required = false, HelpText = "Print entire response.")]
        public bool Verbose { get; set; }
    }
    class Program
    {
        static string AccessToken;
        static string RefreshToken;
        static string ClientId;
        static string TenantId;
        static string Authority;
        static string Resource;

        static string CallApiMethod(string method, string endpoint, string accessToken, string body, string contentType)
        {

            string graphUrl = string.Format("https://graph.microsoft.com/v1.0/{0}", endpoint);
            WebRequest request = WebRequest.Create(graphUrl);
            request.Method = method;
            request.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken));

            if (contentType != null)
            {
                request.ContentType = contentType;
            }

            if (body != null)
            {
                byte[] data = Encoding.ASCII.GetBytes(body);
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);

            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();


            if (response.StatusCode.ToString() == "OK" || response.StatusCode.ToString() == "Created" || response.StatusCode.ToString() == "NoContent")
            {
                return responseFromServer;
            }
            else
            {

                Console.WriteLine(response.StatusCode);
                Console.WriteLine(responseFromServer);
                return null;

            }

        }
        static string GetUpdatedAccessToken()
        {
            Console.WriteLine("Requesting token");
            WebRequest request = WebRequest.Create(Authority + "/oauth2/token");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("client-request-id", "01d1sa19-1337-42d8-1337-82c0d8d3c43e");
            request.Headers.Add("Accept-Charset", "UTF-8");

            String postdata = String.Format("grant_type=refresh_token&client_id={0}&resource=https%3A%2F%2Fgraph.microsoft.com%2F&refresh_token={1}", ClientId, RefreshToken);

            byte[] data = Encoding.ASCII.GetBytes(postdata);

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            //  Retrieve the response
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode.ToString() != "OK")
            {
                Console.WriteLine(response.StatusDescription);
                Console.WriteLine("Failed to retrieve the Access Token, exiting.");
                return null;
            }

            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);

            string responseFromServer = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();

            dynamic JsonData = JsonConvert.DeserializeObject(responseFromServer);
           // Console.WriteLine(JsonData.access_token);
            Console.WriteLine("Token Retrieved Successfully");
            return JsonData.access_token;
        }
        static bool ReadAzureFile(string path)
        {
            String data;
            if (path == null)
            {
                Console.Write("No path specified, using the default one: ");
                string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\')[1];
                path = String.Format("C:\\Users\\{0}\\.azure\\accessTokens.json", username);
                Console.WriteLine(path);
                data = File.ReadAllText(path);
            }
            else
            {
                Console.Write(String.Format("Using Path: {0}", path));
                data = File.ReadAllText(path);
            }

            dynamic jsonData = JsonConvert.DeserializeObject(data);

            foreach (dynamic temp in jsonData)
            {

                string tempResource = temp.resource;
                if (tempResource.Equals("https://graph.microsoft.com/"))
                {
                    AccessToken = temp.accessToken;
                    RefreshToken = temp.refreshToken;
                    Resource = temp.resource;
                    ClientId = temp._clientId;
                    Authority = temp._authority;
                    TenantId = Authority.Split('/')[3];
                    return true;
                }
            }
            return false;



        }

        static bool CreateGlobalAdminUser(string displayName, string userName, string userPrincipal, string userPassword) {
            string UserId;
            dynamic jsonData;
            string responseData;
            string RoleId = null;

            string postdata = @"{{
                      ""accountEnabled"": true,
                      ""displayName"": ""{0}"",
                      ""mailNickname"": ""{1}"",
                      ""userPrincipalName"": ""{2}"",
                      ""passwordProfile"" : {{
                        ""forceChangePasswordNextSignIn"": false,
                        ""password"": ""{3}""
                      }}
            }}";
            postdata = String.Format(postdata, displayName, userName, userPrincipal, userPassword);

            // Create User
            responseData = CallApiMethod("POST", "users", AccessToken, postdata, "application/json");

            jsonData = JsonConvert.DeserializeObject(responseData);
            UserId = jsonData.id;
            Console.WriteLine(UserId);

            responseData = CallApiMethod("GET", "directoryRoles", AccessToken, null, null);
            jsonData = JsonConvert.DeserializeObject(responseData);
            foreach (dynamic temp in jsonData.value)
            {
                string roleName = temp.displayName;
                if (roleName.Equals("Company Administrator"))
                {

                    RoleId = temp.id;
                    Console.WriteLine(RoleId);

                }
            }
            if (RoleId == null)
            {
                Console.WriteLine("Failed to find the Global Administrators Role ID");
                return false;
            }

            string addMemberEndpoint = String.Format("/directoryRoles/{0}/members/$ref", RoleId);

            postdata = @"{{
                ""@odata.id"": ""https://graph.microsoft.com/v1.0/directoryObjects/{0}""
                }}
            ";
            postdata = String.Format(postdata, UserId);
            CallApiMethod("POST", addMemberEndpoint, AccessToken, postdata, "application/json");
            Console.WriteLine("User with id {0} was added successfully to the Global Administrator Group", UserId);
            return true;
        }
        
        static int Main(string[] args)
        {


            
                return CommandLine.Parser.Default.ParseArguments<AddUserOptions,TokenOptions>(args)
                  .MapResult(
                    (AddUserOptions opts) => AddUser(opts),

                    (TokenOptions opts) => GetToken(opts),
                    errs => 1);
            
        }

        static int GetToken(TokenOptions tokenOptions)
        {
            ReadAzureFile(null);
            AccessToken = GetUpdatedAccessToken();
            Console.WriteLine("Access token : {0}",AccessToken);
            return 0;
        }
        static int AddUser(AddUserOptions opts)
        {
            ReadAzureFile(null);
            AccessToken = GetUpdatedAccessToken();

            Console.WriteLine("Creating Global Administrator with provided information.");
            CreateGlobalAdminUser(opts.DisplayName, opts.Username, opts.UserPrincipal, opts.Password);
            return 0;
        }
    }
}
