using System;
using SugarTools;

namespace sugarRestExample
{
    class Program
    {
        static void Main(string[] args)
        {
            //Define object and authenticate to sugar instance
            SugarRest sugar = new SugarRest(new Uri("http://lin-web-01.puc.blockken.local/ent7621"));
            sugar.login("admin", "admin");
            
            //Get the user's preferences
            Console.WriteLine(sugar.me());
            
            //Retrieve a new access token and refresh token with the current refresh token
            //Necesary when access token has expired
            sugar.refresh();

            //Perform a global search
            dynamic search = sugar.search("A");
            foreach (dynamic sresult in search.records)
            {
                Console.WriteLine(sresult.name + " " + sresult.id + " " + sresult._module);
            }
            
            //Retrieve a specefic record with id from the accounts module
            Console.WriteLine(sugar.retrieveRecord("Accounts", "cf5b685c-eb97-797f-13b8-5703a6683a5e"));
            
            
            //Search for users, return only the 5 results and only fields name and id
            dynamic result = sugar.searchUsers("a",5,0,"name,id");
            foreach (dynamic r in result.records)
            {
                Console.WriteLine(r.name + " " + r.id);
            }

            //Search for a records in a specefic module
            Console.WriteLine(sugar.searchModule("Accounts", "SugarCRM"));

            //Log an entry in the sugarcrm log. Must be authenticated first
            sugar.logMessage("Fatal log line", "fatal");
            
            //Create a new account record with a newly created meetings
            var record = new
            {
                name = "Example C# Account",
                description = "Created from SugarRest C# Library",
                meetings = new
                {
                    create = new[] {
                        new { name = "Test C# Meeting", date_start = "2016-03-18T10:00:00-00:00" },
                        new { name = "Follow up C# Test Meeting", date_start = "2016-03-22T10:00:00-00:00"},
                    }
                }
            };
            Console.WriteLine(sugar.createRecord("Accounts", record));

            //Update meeting record
            var uData = new
            {
                name = "Updated meeting",
                date_start = "2016-04-04T14:30:00-00:00"
            };

            Console.WriteLine(sugar.updateRecord("Meetings", "94dc6611-e085-bbbd-c98f-5703aa39bfdb", uData));

            //Delete record
            sugar.deleteRecord("Meetings", "94dc6611-e085-bbbd-c98f-5703aa39bfdb");

            Console.ReadKey();
        }
    }
}
