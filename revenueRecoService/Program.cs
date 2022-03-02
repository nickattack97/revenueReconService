using System;
using System.Collections;
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;

namespace revenueRecoService
{
    class Program
    {
        public static DateTime timeNow = DateTime.Now;

        static void Main(string[] args)
        {
            Program.GetTransactions();
        }

        private static void InsertFccTransactions(string trn_ref_no_dt)
        {
            string sql = "INSERT ALL "+ trn_ref_no_dt +" SELECT * FROM dual";


            using (OracleConnection con = new OracleConnection(ConfigurationManager.ConnectionStrings["fccDB"].ConnectionString))
            {
                con.Open();
                try
                {
                    using (OracleCommand command = new OracleCommand(sql, con))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    createEvent(e.Message + " when inserting record" + trn_ref_no_dt, "Error");

                    Program.Mail(e.Message + (object)DateTime.Now);
                }
            }
        }

        public static void GetTransactions()
        {
            OracleConnection conn = new OracleConnection(ConfigurationManager.ConnectionStrings["fccDB"].ConnectionString);

            Dictionary<string, DateTime> recordsPicked = new Dictionary<string, DateTime>(); 

            string trn_ref_no = string.Empty;
            string prod_code = string.Empty;
            string txn_ccy = string.Empty;
            string channel = string.Empty;
            decimal txn_amount = decimal.Zero;
            decimal txn_charge = decimal.Zero;
            DateTime txn_date;
            string user_id = string.Empty;
            string customer_type = string.Empty;
            string customer_cat = string.Empty;
            string charge_code = string.Empty;
            string code = string.Empty;
            decimal cal_charge = decimal.Zero;


            OracleDataReader Reader;
            try
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand(ConfigurationManager.AppSettings["getRevenueTransactions"], conn))
                {
                    cmd.CommandTimeout = 0;
                    Reader = cmd.ExecuteReader();
                    while (Reader.Read())
                    {
                        trn_ref_no = Reader["TRN_REF_NO"].ToString();
                        prod_code = Reader["PRODUCT_CODE"].ToString();
                        txn_ccy = Reader["TXN_CCY"].ToString();
                        user_id = Reader["USER_ID"].ToString();
                        txn_amount = Reader["TXN_AMOUNT"] == DBNull.Value ? 0 : Convert.ToDecimal(Reader["TXN_AMOUNT"]);
                        txn_charge = Reader["TXN_CHARGE"] == DBNull.Value ? 0 : Convert.ToDecimal(Reader["TXN_CHARGE"]);
                        txn_date = Convert.ToDateTime(Reader["TXN_DATE"]);
                        customer_type = Reader["CUSTOMER_TYPE"].ToString();
                        customer_cat = Reader["CUSTOMER_CATEGORY"].ToString();
                        charge_code = Reader["CHARGE_CODE"].ToString();
                        channel = Reader["CHANNEL"].ToString();
                        code = Reader["CODE"].ToString();
                        cal_charge = Reader["CALC_CHARGE"] == DBNull.Value ? 0 : Convert.ToDecimal(Reader["CALC_CHARGE"]);


                        if (saveRecords(trn_ref_no, prod_code, txn_ccy, channel, txn_amount, txn_charge, txn_date, user_id, customer_type, customer_cat, charge_code, code, cal_charge) != 0)
                        {
                            recordsPicked.Add(trn_ref_no, txn_date);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Program.createEvent(e.StackTrace, "Error");
                Program.Mail(e.Message + (object)DateTime.Now);
            }

            finally
            {
                conn.Close();
            }

            if (recordsPicked.Count > 0)
            {
                createRecordsForInsert(recordsPicked);
            }

        }

        private static int saveRecords(string trn_ref_no, string prod_code, string txn_ccy, string channel, decimal txn_amount, decimal txn_charge, DateTime txn_date, string user_id, string customer_type, string customer_cat, string charge_code, string code, decimal cal_charge)
        {
            int response = 0;
            string userid = "SYSTEM";

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["EPcon"].ConnectionString))
            {
                con.Open();

                using (SqlCommand sqlCommand = new SqlCommand("[spInsertRevenueTrans]", con))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Clear();
                    sqlCommand.Parameters.AddWithValue("@TRN_REF_NO",trn_ref_no);
                    sqlCommand.Parameters.AddWithValue("@PRODUCT_CODE", prod_code);
                    sqlCommand.Parameters.AddWithValue("@TXN_CCY", txn_ccy);
                    sqlCommand.Parameters.AddWithValue("@USER_ID", user_id);
                    sqlCommand.Parameters.AddWithValue("@TXN_AMOUNT", txn_amount);
                    sqlCommand.Parameters.AddWithValue("@TXN_CHARGE", txn_charge);
                    sqlCommand.Parameters.AddWithValue("@TXN_DATE", txn_date);
                    sqlCommand.Parameters.AddWithValue("@CUSTOMER_TYPE", customer_type);
                    sqlCommand.Parameters.AddWithValue("@CUSTOMER_CATEGORY", customer_cat);
                    sqlCommand.Parameters.AddWithValue("@CHARGE_CODE", charge_code);
                    sqlCommand.Parameters.AddWithValue("@CHANNEL", channel);
                    sqlCommand.Parameters.AddWithValue("@CODE", code);
                    sqlCommand.Parameters.AddWithValue("@CALC_CHARGE", cal_charge);
                    sqlCommand.Parameters.AddWithValue("@DATE_ADDED", timeNow);
                    sqlCommand.Parameters.AddWithValue("@USERID", "SYSTEM");

                    SqlDataReader myData = sqlCommand.ExecuteReader();

                    while (myData.Read())
                    {
                        response = Convert.ToInt32(myData["ID"]);

                    }
                }

                con.Close();

                return response;

            }
        }

        private static void createRecordsForInsert(Dictionary<string,DateTime> recordsPicked)
        {
            int cnt = 0;
            int loop = 0;
            string terminator = string.Empty;
            string values = string.Empty;
            int numOfRecords = recordsPicked.Count;

            foreach (KeyValuePair<string, DateTime> x1 in recordsPicked)
            {

                cnt++;
                loop++;
                if (cnt < numOfRecords && loop < 1000)
                {
                    terminator = ",";
                }
                else
                {
                    terminator = string.Empty;
                    loop = 0;
                }

                values += " into RECON_SYSTEM_CONTROL(TRN_REF_NO, TRN_DT) VALUES('" + x1.Key + "','" + x1.Value.ToString("dd/MMM/yyyy") + "') ";

                if (loop == 0)
                {
                    InsertFccTransactions(values);
                    values = string.Empty;
                }
            }


        }
 

        private static void createEvent(string sEvent, string sEventType)
        {
            string source = "RevenueReconGetFlex";
            string logName = "RevenueReconGetFlex";
            if (!EventLog.SourceExists(source))
                EventLog.CreateEventSource(source, logName);
            if (sEventType.Equals("Error"))
                EventLog.WriteEntry(source, sEvent, EventLogEntryType.Error);
            else if (sEventType.Equals("Warning"))
                EventLog.WriteEntry(source, sEvent, EventLogEntryType.Warning);
            else if (sEventType.Equals("Success"))
            {
                EventLog.WriteEntry(source, sEvent, EventLogEntryType.Information);
            }
            else
            {
                if (!sEventType.Equals("Audit"))
                    return;
                EventLog.WriteEntry(source, sEvent, EventLogEntryType.SuccessAudit);
            }
        }


        public static void Mail(string mess)
        {
            MailMessage message = new MailMessage();
            message.IsBodyHtml = true;
            message.From = new MailAddress("RevenueReconGetFlex@cbz.co.zw");
            message.To.Add(ConfigurationManager.AppSettings["emailGroup"]);
            message.Subject = "RevenueReconGetFlex";
            message.Body = mess;
            try
            {
                new SmtpClient(ConfigurationManager.AppSettings["smtpip"], int.Parse(ConfigurationManager.AppSettings["smtpport"])).Send(message);
            }
            catch (Exception ex)
            {
                try
                {
                    new SmtpClient(ConfigurationManager.AppSettings["smtpip2"], int.Parse(ConfigurationManager.AppSettings["smtpport2"])).Send(message);
                }
                catch
                {
                    Program.createEvent(ex.Message, string.Concat((object)EventLogEntryType.Error));
                }
                Program.createEvent(ex.Message, string.Concat((object)EventLogEntryType.Error));
            }
        }

    }
}
