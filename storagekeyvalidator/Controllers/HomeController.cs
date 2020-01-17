using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace storagekeyvalidator.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string AzureWebJobsDashboard = WebConfigurationManager.AppSettings["AzureWebJobsDashboard"];
            string AzureWebJobsStorage = WebConfigurationManager.AppSettings["AzureWebJobsStorage"];
            string WEBSITE_CONTENTAZUREFILECONNECTIONSTRING = WebConfigurationManager.AppSettings["WEBSITE_CONTENTAZUREFILECONNECTIONSTRING"];

          
            Dictionary<string, string> dictConnections = new Dictionary<string, string>();
            dictConnections.Add("AzureWebJobsDashboard", AzureWebJobsDashboard);
            dictConnections.Add("AzureWebJobsStorage", AzureWebJobsStorage);
            dictConnections.Add("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING", WEBSITE_CONTENTAZUREFILECONNECTIONSTRING);
            
            DataTable tblKey = new DataTable();

            DataColumn columnkeyname;
            columnkeyname = new DataColumn();
            columnkeyname.DataType = System.Type.GetType("System.String");
            columnkeyname.ColumnName = "StorageKey";
            tblKey.Columns.Add(columnkeyname);


            DataColumn columnresult;
            columnresult = new DataColumn();
            columnresult.DataType = System.Type.GetType("System.String");
            columnresult.ColumnName = "Result";
            tblKey.Columns.Add(columnresult);

            DataColumn columnmessage;
            columnmessage = new DataColumn();
            columnmessage.DataType = System.Type.GetType("System.String");
            columnmessage.ColumnName = "Description";
            tblKey.Columns.Add(columnmessage);


            foreach (var item in dictConnections)
            {
                CloudStorageAccount storageAccount;

                if( !string.IsNullOrEmpty(item.Value) )
                {
                    if (CloudStorageAccount.TryParse(item.Value, out storageAccount))
                    {

                        if (blobonly(storageAccount))
                        {
                            DataRow row = tblKey.NewRow();
                            row["StorageKey"] = item.Key;
                            row["Result"] = "Failed";
                            row["Description"] = "Function app requires a general Azure Storage account, which supports Azure Blob, Queue, Files, and Table storage.Refer <a href='https://docs.microsoft.com/en-us/azure/azure-functions/functions-scale#storage-account-requirements' target='_blank'>Storage Requirements</a>" ;
                            tblKey.Rows.Add(row);

                        }
                        else
                        {
                            try
                            {
                                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                                CloudBlobContainer cloudBlobContainer = blobClient.GetContainerReference("testblob" + Guid.NewGuid().ToString());
                                var results = cloudBlobContainer.ListBlobsSegmented(null, null);


                                DataRow row = tblKey.NewRow();
                                row["StorageKey"] = item.Key;
                                row["Result"] = "succeeded";
                                row["Description"] = "The storage connection string is valid and no further action needed";
                                tblKey.Rows.Add(row);


                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("403"))
                                {
                                    DataRow row = tblKey.NewRow();
                                    row["StorageKey"] = item.Key;
                                    row["Result"] = "Failed";
                                    row["Description"] = "The storage connection string account Key is invalid, the server failed to authenticate the request. The access keys have either expired or have been revoked by your admin. Please contact your admin to generate a new storage connection key. Refer <a href= 'https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings#azurewebjobsstorage' target='_blank'> Function Keys app settings </a> and <a href='https://docs.microsoft.com/en-us/azure/azure-functions/functions-scale#storage-account-requirements' target='_blank'>Storage Requirements</a>";
                                    tblKey.Rows.Add(row);
                                }

                                if (ex.Message.Contains("404"))
                                {

                                    DataRow row = tblKey.NewRow();
                                    row["StorageKey"] = item.Key;
                                    row["Result"] = "Succeeded";
                                    row["Description"] = "The storage connection string is valid and no further action needed";
                                    tblKey.Rows.Add(row);
                                }
                            }
                        }
                    }
                    else
                    {
                        DataRow row = tblKey.NewRow();
                        row["StorageKey"] = item.Key;
                        row["Result"] = "Failed";
                        row["Description"] = "The storage connection string couldnt be parsed, check the syntax of the string. you can try generating a new connection string for the storage account and reconfigure the app setting. Please refer <a href= 'https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings#azurewebjobsstorage' target='_blank'> Function Keys app settings </a> and <a href='https://docs.microsoft.com/en-us/azure/azure-functions/functions-scale#storage-account-requirements' target='_blank'>Storage Requirements</a>";
                        tblKey.Rows.Add(row);
                    }
                }

                storageAccount = null;

            }

            if(tblKey.Rows.Count < 1)
            {
                DataRow row = tblKey.NewRow();
                row["StorageKey"] = "NULL";
                row["Result"] = "No Storage Keys Found";
                row["Description"] = "Azure Function runtime must be associated with a storage account, please refer <a href= 'https://docs.microsoft.com/en-us/azure/azure-functions/functions-app-settings#azurewebjobsstorage' target='_blank'> Function Keys app settings </a> and <a href='https://docs.microsoft.com/en-us/azure/azure-functions/functions-scale#storage-account-requirements' target='_blank'>Storage Requirements</a>";
                tblKey.Rows.Add(row);

            }



            return View(tblKey);
        }

        public bool blobonly(CloudStorageAccount storage)
        {
            bool result = false;
            try
            {
                Dns.GetHostEntry(storage.BlobEndpoint.Host);
                Dns.GetHostEntry(storage.FileEndpoint.Host);
                Dns.GetHostEntry(storage.QueueEndpoint.Host);
                Dns.GetHostEntry(storage.TableEndpoint.Host);
            }
            catch
            {
                result = true;
            }

            return result;
        }

    }
}