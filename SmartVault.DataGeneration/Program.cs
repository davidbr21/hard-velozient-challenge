using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartVault.Library;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Transactions;
using System.Xml.Serialization;

namespace SmartVault.DataGeneration
{
    partial class Program
    {
        // Date Format used in CreatedOn columns of each BussinessObject, and stored in DateOfBirth field in User table
        static string dateFormat = "yyyy-MM-dd";

        static void Main(string[] args)
        {
            try //Try-Catch to handle general exceptions
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json").Build();

                SQLiteConnection.CreateFile(configuration["DatabaseFileName"]);

                string fileContent = $"Smith Property - This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}This is my test document{Environment.NewLine}";
                File.WriteAllText("TestDoc.txt", fileContent);

                var documentPath = new FileInfo("TestDoc.txt").FullName;
                var documentLength = new FileInfo(documentPath).Length;

                //Stores the content of files that matches given parameter. Default "Smith Property"
                string parametrizedFileContent = "";

                //Initializing connection
                using (var connection = new SQLiteConnection(string.Format(configuration?["ConnectionStrings:DefaultConnection"] ?? "", configuration?["DatabaseFileName"])))
                {
                    connection.Open();
                    //Initializing db transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try //Try-catch to handle transaction errors
                        {
                            var files = Directory.GetFiles(@"..\..\..\..\BusinessObjectSchema"); //List files from BussinessObjects
                        
                            for (int i = 0; i < files.Length; i++)
                            {
                                var serializer = new XmlSerializer(typeof(BusinessObject));
                                var businessObject = serializer.Deserialize(new StreamReader(files[i])) as BusinessObject;
                                connection.Execute(businessObject?.Script);
                            }

                            var documentNumber = 0; //Stores the total count of documents written
                            int accountsToGenerate = int.Parse(configuration["TestingRowsToGenerate:Accounts"]);
                            int docsToGenerate = int.Parse(configuration["TestingRowsToGenerate:Documents"]);

                            for (int i = 0; i < 100; i++)
                            {
                                var randomDayIterator = RandomDay().GetEnumerator();
                                randomDayIterator.MoveNext();
                                //Inserting Accounts & Users
                                connection.Execute($"INSERT INTO Account (Id, Name, CreatedOn) VALUES('{i}','Account{i}', '{DateTime.Now.ToString(dateFormat)}')");
                                connection.Execute($"INSERT INTO User (Id, FirstName, LastName, DateOfBirth, AccountId, Username, Password, CreatedOn) VALUES('{i}','FName{i}','LName{i}','{randomDayIterator.Current.ToString(dateFormat)}','{i}','UserName-{i}','e10adc3949ba59abbe56e057f20f883e', '${DateTime.Now.ToString(dateFormat)}')");
                                for (int d = 0; d < 10000; d++, documentNumber++)
                                {
                                    parametrizedFileContent += validateSpecificParameter(d, i, fileContent, configuration);
                                    //Inserting documents, and associates an account to each record entered
                                    connection.Execute($"INSERT INTO Document (Id, Name, FilePath, Length, AccountId, CreatedOn) VALUES('{documentNumber}','Document{i}-{d}.txt','{documentPath}','{documentLength}','{i}', '${DateTime.Now.ToString(dateFormat)}')");
                                }
                            }

                            transaction.Commit();
                        } catch(Exception ex)
                        {
                            transaction.Rollback();
                            throw new TransactionException("An error has occured while trying to create and/or executing the transaction");
                        }
                    }


                    var accountData = connection.Query("SELECT count(*) FROM Account;");
                    Console.WriteLine($"AccountCount: {JsonConvert.SerializeObject(accountData)}");
                    var documentData = connection.Query("SELECT count(*) FROM Document LIMIT 100;");
                    Console.WriteLine($"DocumentCount: {JsonConvert.SerializeObject(documentData)}");
                    var userData = connection.Query("SELECT count(*) FROM User;");
                    Console.WriteLine($"UserCount: {JsonConvert.SerializeObject(userData)}");

                    connection.Close();
                }

                //If given parameter matched any file in any account, it'll create the ParametrizedFile.txt
                if(!string.IsNullOrEmpty(parametrizedFileContent))
                    File.WriteAllText("ParametrizedFile.txt", parametrizedFileContent);

            } catch(Exception ex ) {
                Console.WriteLine(ex.Message);
            }
        }

        static IEnumerable<DateTime> RandomDay()
        {
            DateTime start = new DateTime(1985, 1, 1);
            Random gen = new Random();
            int range = (DateTime.Today - start).Days;
            while (true)
                yield return start.AddDays(gen.Next(range));
        }

        /// <summary>
        /// BUSSINESS REQUIREMENT: Validates if each third document of an account contains certain string. Default: "Smith Parameter"
        /// </summary>
        /// <param name="docNumber">The number of document of the account being processed</param>
        /// <returns>Returns </returns>
        static string validateSpecificParameter(int docNumber, int accountId, string fileContent, dynamic configuration)
        {
            int fileNumber = int.Parse(configuration["TestingFileParameters:FileNumber"]); //Number of file of the account related that should be validated (Since the validation should be done every X document of an account).
            if (docNumber == fileNumber && fileContent.Contains(configuration["TestingFileParameters:FileContent"]))
            {
                return $"Document Number {docNumber} from Account Id #{accountId} {Environment.NewLine}{Environment.NewLine}{fileContent}{Environment.NewLine}";
            }
            return "";
        }
    }
}