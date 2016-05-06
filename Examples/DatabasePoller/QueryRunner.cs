using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using ServiceManager;

namespace DatabasePoller
{
    public class QueryRunner
    {
        public string DropPoint { get; set; }

        public static void Run()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["XmlSource"].ConnectionString;
            string dropPoint = ConfigurationManager.AppSettings["DropPoint"];
            string query = ConfigurationManager.AppSettings["Query"];
            ServiceContext.LogInfo("Will query database {0} and store files in {1}", connectionString, dropPoint);

            try {
                new QueryRunner(dropPoint).CreateXML(query, connectionString);
            } catch (Exception ex) {
                ServiceContext.LogWarning(ex.Message);
            }
        }

        public QueryRunner(string dropPoint)
        {
            DropPoint = dropPoint;
        }

        private void CreateXML(string queryString, string connectionString)
        {
            ServiceContext.LogInfo("Starting query");
            using (SqlConnection connection = new SqlConnection(connectionString)) {
                connection.Open();
                SqlCommand command = new SqlCommand(queryString, connection);
                XmlReader reader = command.ExecuteXmlReader();
                WriteXmlToQueue(reader);
            }
        }
        private void WriteXmlToQueue(XmlReader r)
        {
            ServiceContext.LogInfo("Writing to queue...");

            string fileName =  Guid.NewGuid().ToString("n") + ".tmp";
            var fullPath = Path.Combine(DropPoint, fileName);

            if (!Directory.Exists(DropPoint)) {
                ServiceContext.LogInfo("Drop point {0} does not exist. Creating.", DropPoint);
                Directory.CreateDirectory(DropPoint);
            }

            using (XmlWriter writer = XmlWriter.Create(File.OpenWrite(fullPath), new XmlWriterSettings{CloseOutput=true})) {
                writer.WriteNode(r, true);
            }

            File.Move(fullPath, Path.ChangeExtension(fullPath, "xml"));
            ServiceContext.LogInfo("Wrote file {0}", Path.ChangeExtension(fullPath, "xml"));
        }
    }
}
