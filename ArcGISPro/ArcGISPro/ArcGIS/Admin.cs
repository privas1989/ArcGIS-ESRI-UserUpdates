using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace ArcGISPro.ArcGIS
{
    public class Admin
    {
        private string username;
        private string password;
        private string portal_url;
        private string token;
        private string portal_id;

        public Admin(string username, string password, string portal_url)
        {
            this.username = username;
            this.password = password;
            this.portal_url = portal_url;
        }

        public void Connect(int token_expiration_time)
        {
            string json_retrieved = null;
            var parameters = new Dictionary<string, string>();
            parameters.Add("username", this.username);
            parameters.Add("password", this.password);
            parameters.Add("client", "referer");
            parameters.Add("referer", this.portal_url);
            parameters.Add("expiration", token_expiration_time.ToString());
            parameters.Add("f", "json");

            var encoded_content = new FormUrlEncodedContent(parameters);

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var result = httpClient.PostAsync(portal_url + "/sharing/rest/generateToken?", encoded_content);
                    json_retrieved = result.Result.Content.ReadAsStringAsync().Result;
                }

                this.token = JObject.Parse(json_retrieved)["token"].ToString();
                this.portal_id = GetPortalId();
            }
            catch
            {

            }
        }

        private string GetPortalId()
        {
            string id = null;
            string json_retrieved;
            var parameters = new Dictionary<string, string>();
            parameters.Add("token", this.token);
            parameters.Add("f", "json");

            var encoded_content = new FormUrlEncodedContent(parameters);

            if (!String.IsNullOrEmpty(this.token))
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var result = httpClient.PostAsync(portal_url + "/sharing/rest/portals/self?", encoded_content);
                        json_retrieved = result.Result.Content.ReadAsStringAsync().Result;
                    }

                    id = JObject.Parse(json_retrieved)["id"].ToString();
                }
                catch
                {

                }
            }

            return id;
        }

        private string GetUsersJSON(int pointer, int num)
        {
            string users = null;
            string json_retrieved;
            var parameters = new Dictionary<string, string>();
            parameters.Add("token", this.token);
            parameters.Add("f", "json");
            parameters.Add("start", pointer.ToString());
            parameters.Add("num", num.ToString());

            var encoded_content = new FormUrlEncodedContent(parameters);

            if (!String.IsNullOrEmpty(this.token) && !String.IsNullOrEmpty(this.portal_id))
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var result = httpClient.PostAsync(portal_url + "/sharing/rest/portals/" + this.portal_id + "/users?", encoded_content);
                        json_retrieved = result.Result.Content.ReadAsStringAsync().Result;
                    }

                    users = json_retrieved;
                }
                catch
                {

                }
            }

            return users;
        }

        public List<string> GetUserList()
        {
            List<string> user_list = new List<string>();

            bool more_users = true;
            int pointer = 0;
            int num = 100;
            do
            {
                string users_json = GetUsersJSON(pointer, num);

                JObject users = JObject.Parse(users_json);

                foreach (JObject user in users["users"])
                {
                    user_list.Add(user["username"].ToString());
                }

                pointer = (int)users["nextStart"];
                if (pointer == -1)
                {
                    more_users = false;
                }

            }
            while (more_users);

            return user_list;
        }

        public JObject GetEntitlements(List<string> products)
        {
            JObject entitlements = new JObject();
            string json_retrieved;
            Dictionary<string, string> products_dict = new Dictionary<string, string>();
            products_dict.Add("pro", "2d2a9c99bb2a43548c31cd8e32217af6");
            products_dict.Add("geo", "5e99f4fa519949209cd3da2966fd543b");
            products_dict.Add("app", "6a05f1bb2b60461fa702c648bff17c51");
            products_dict.Add("cao", "7b504b19ddbd4f0db06e9a16eebb5efc");
            products_dict.Add("bao", "ed12fda02a0d4bd08f23dbc879bba00a");

            var parameters = new Dictionary<string, string>();
            parameters.Add("token", this.token);
            parameters.Add("f", "json");

            var encoded_content = new FormUrlEncodedContent(parameters);

            if (!String.IsNullOrEmpty(this.token))
            {
                foreach (string product in products)
                {
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            var result = httpClient.PostAsync(portal_url + "/sharing/rest/content/listings/" +
                                products_dict[product] +
                                "/userEntitlements?", encoded_content);
                            json_retrieved = result.Result.Content.ReadAsStringAsync().Result;
                            JObject entitlement = JObject.Parse(json_retrieved);
                            entitlements.Add(product, entitlement["userEntitlements"]);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return entitlements;
        }

        public bool SetEntitlements(JObject new_users)
        {
            bool set = false;
            Dictionary<string, string> products_dict = new Dictionary<string, string>();
            products_dict.Add("pro", "2d2a9c99bb2a43548c31cd8e32217af6");
            products_dict.Add("geo", "5e99f4fa519949209cd3da2966fd543b");
            products_dict.Add("app", "6a05f1bb2b60461fa702c648bff17c51");
            products_dict.Add("cao", "7b504b19ddbd4f0db06e9a16eebb5efc");
            products_dict.Add("bao", "ed12fda02a0d4bd08f23dbc879bba00a");

            Dictionary<string, string[]> product_tags = new Dictionary<string, string[]>();
            product_tags.Add("pro", new string[] { "3DAnalystN", "dataReviewerN", "desktopAdvN", "geostatAnalystN", "networkAnalystN", "spatialAnalystN", "workflowMgrN" });
            product_tags.Add("geo", new string[] { "GeoPlanner" });
            product_tags.Add("app", new string[] { "appstudiostd" });
            product_tags.Add("cao", new string[] { "CommunityAnlyst" });
            product_tags.Add("bao", new string[] { "BusinessAnlyst" });

            if (!String.IsNullOrEmpty(this.token))
            {
                foreach (var product in new_users)
                {
                    string product_num = products_dict[product.Key];
                    JArray entitlements_tags = JArray.FromObject(product_tags[product.Key]);
                    JObject user_entitlements = new JObject();

                    user_entitlements["users"] = product.Value;
                    user_entitlements["entitlements"] = entitlements_tags;

                    try
                    {
                        string json_retrieved;
                        var parameters = new Dictionary<string, string>();
                        parameters.Add("userEntitlements", user_entitlements.ToString());
                        parameters.Add("token", this.token);
                        parameters.Add("f", "json");

                        var encoded_content = new FormUrlEncodedContent(parameters);

                        using (var httpClient = new HttpClient())
                        {
                            var result = httpClient.PostAsync(portal_url + "/sharing/rest/content/listings/" +
                                product_num +
                                "/provisionUserEntitlements", encoded_content);
                            json_retrieved = result.Result.Content.ReadAsStringAsync().Result;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return set;
        }

        public bool SetUserType(string username, string user_type)
        {
            bool success = false;

            if (!String.IsNullOrEmpty(this.token))
            {
                try
                {
                    string json_retrieved;
                    var parameters = new Dictionary<string, string>();
                    parameters.Add("userType", user_type);
                    parameters.Add("token", this.token);
                    parameters.Add("f", "json");

                    var encoded_content = new FormUrlEncodedContent(parameters);

                    using (var httpClient = new HttpClient())
                    {
                        var result = httpClient.PostAsync(portal_url + "/sharing/rest/community/users/" +
                            username +
                            "/update", encoded_content);
                        json_retrieved = result.Result.Content.ReadAsStringAsync().Result;
                        Console.WriteLine(json_retrieved);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return success;
        }
    }
}
