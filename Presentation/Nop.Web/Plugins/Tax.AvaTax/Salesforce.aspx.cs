
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services.Protocols;
//using SalesforceService; // this is for sandbox account
using SalesforceServiceProduction; // this is for production 
using CMS.EventLog;


public partial class Salesforce : System.Web.UI.Page
{



    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        var boolLogin = login();
        if (boolLogin == true)
        {
            //rediect user for testing if login succussful
            Response.Redirect("https://dev.meforum.org/confirmation.aspx");
        }
        else
        {
            // redirect user if login failed
            Response.Redirect("https://google.com");
        }
    }

    public bool login()
    {
        try
        {
            EventLogProvider.LogInformation("CMSWebParts_MEFDonation_Donation", "501", "In login");

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string username = txtUserName.Text;

            string password = txtPassword.Text;

            // Create a service object

            var binding = new SforceService();

            EventLogProvider.LogInformation("CMSWebParts_MEFDonation_Donation", "502", "In login");
            // Timeout after a minute
            binding.Timeout = 60000;

            // Try logging in  
            LoginResult lr;


            Console.WriteLine("\nLogging in...\n");
            lr = binding.login(username, password);



            // Check if the password has expired
            if (lr.passwordExpired)
            {
                Console.WriteLine("An error has occurred. Your password has expired.");

                return false;

            }

            /** Once the client application has logged in successfully, it will use

            * the results of the login call to reset the endpoint of the service

            * to the virtual server instance that is servicing your organization

            */

            // Save old authentication end point URL

            String authEndPoint = binding.Url;

            // Set returned service endpoint URL

            binding.Url = lr.serverUrl;

            /** The sample client application now has an instance of the SforceService

             * that is pointing to the correct endpoint. Next, the sample client

             * application sets a persistent SOAP header (to be included on all

             * subsequent calls that are made with SforceService) that contains the

             * valid sessionId for our login credentials. To do this, the sample

             * client application creates a new SessionHeader object and persist it to

             * the SforceService. Add the session ID returned from the login to the

             * session header

             */

            binding.SessionHeaderValue = new SessionHeader();

            binding.SessionHeaderValue.sessionId = lr.sessionId;

            npe03__Recurring_Donations_Settings__c obj = new npe03__Recurring_Donations_Settings__c();

            //printUserInfo(lr, authEndPoint);

            // Return true to indicate that we are logged in, pointed 

            // at the right URL and have our security token in place.   

        }
        // ApiFault is a proxy stub generated from the WSDL contract when    

        // the web service was imported
        catch (SoapException ex)
        {
            EventLogProvider.LogException("Salesforce:login", "500", ex);
            // Write the fault code to the console
            // Write the fault message to the console

            //Console.WriteLine("An unexpected error has occurred: " + e.Message);

            // Write the stack trace to the console

            //Console.WriteLine(ex.StackTrace);

            // Return False to indicate that the login was not successful

            return false;

        }
        //querySample_SreachDonor();
        //querySample_InsertDonor();
        //querySample_InsertDonation();
        return true;


    }


}