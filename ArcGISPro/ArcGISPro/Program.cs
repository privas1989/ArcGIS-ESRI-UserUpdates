using System;
using log4net;
using log4net.Config;
using System.Reflection;
using System.IO;
using ArcGISPro.ArcGIS;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ArcGISPro
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log4net.config"));

            log.Info("Application started.");

            UpdateUserEntitlements();

            UpdateEsriAccess();

            Console.ReadLine();
        }

        public static void UpdateUserEntitlements()
        {
            bool arc_gis_pro = true;
            bool geo_planner = false;
            bool app_studio = false;
            bool community_analyst = false;
            bool business_analyst = false;

            List<string> products = new List<string>();
            if (arc_gis_pro) { products.Add("pro"); }
            if (geo_planner) { products.Add("geo"); }
            if (app_studio) { products.Add("app"); }
            if (community_analyst) { products.Add("cao"); }
            if (business_analyst) { products.Add("bao"); }

            ArcGIS.Admin adm = new Admin("arc-gis-admin", "keepitsecretkeepitsafe", "https://www.arcgis.com");
            adm.Connect(60);

            List<string> user_list = adm.GetUserList();

            JObject product_entitlements =  adm.GetEntitlements(products);
            JObject all_product_users = new JObject();

            foreach (string product in products)
            {
                List<string> new_users = new List<string>();
                JObject product_users = new JObject();
                
                foreach (string user in user_list)
                {
                    bool found = false;
                    foreach (JObject entitlement in product_entitlements[product])
                    {
                        if (user.Equals(entitlement["username"].ToString()))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        new_users.Add(user);
                    }
                }

                all_product_users[product] = JArray.FromObject(new_users);
            }

            Console.WriteLine(all_product_users.ToString());
            adm.SetEntitlements(all_product_users);
        }

        public static void UpdateEsriAccess()
        {
            ArcGIS.Admin adm = new Admin("arc-gis-admin", "keepitsecretkeepitsafe", "https://www.arcgis.com");
            adm.Connect(60);

            // set userType to 'both' to enable Esri Access or 'arcgisonly' to disable
            string user_type = "both";

            List<string> user_list = adm.GetUserList();

            foreach (string user in user_list)
            {
                adm.SetUserType(user, user_type);
            }
        }
    }
}
