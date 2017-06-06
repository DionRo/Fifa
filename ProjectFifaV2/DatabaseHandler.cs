using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Data;

namespace ProjectFifaV2
{
    class DatabaseHandler
    {
        private SqlConnection con;

        public DatabaseHandler()
        {
            //SqlCeEngine engine = new SqlCeEngine(@"Data Source=.\DB.sdf");
            //engine.Upgrade(@"Data Source=.\DB2.sdf");

            string Path = Environment.CurrentDirectory;
            string[] appPath = Path.Split(new string[] { "bin" }, StringSplitOptions.None);
            AppDomain.CurrentDomain.SetData("DataDirectory", appPath[0]);

            con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename='|DataDirectory|\db.mdf';Integrated Security=True;Connect Timeout=30");
        }

        public void TestConnection()
        {
            bool open = false;
            
            try
            {
                con.Open();
            }
            catch (Exception ex)
            {
                if (open)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            finally
            {
                if (con.State == System.Data.ConnectionState.Open)
                {
                    open = true;
                }
                con.Close();
            }

            if (!open)
            {
                Application.Exit();
            }
        }

        public void OpenConnectionToDB()
        {
            con.Open();
        }

        public void CloseConnectionToDB()
        {
            con.Close();
        }

        public System.Data.DataTable FillDT(string query)
        {
            TestConnection();
            OpenConnectionToDB();

            SqlDataAdapter dataAdapter = new SqlDataAdapter(query, GetCon());
            DataTable dt = new DataTable();
            dataAdapter.Fill(dt);
            
            CloseConnectionToDB();

            return dt;
        }

        public void TruncateTable(string table)
        {
            using (SqlCommand cmd = new SqlCommand(string.Format("TRUNCATE TABLE {0}", table), this.GetCon()))
            {
                this.TestConnection();
                this.OpenConnectionToDB();

                cmd.ExecuteNonQuery();

                this.CloseConnectionToDB();
            }
        }

        public void EditRow(string table, string row, string value)
        {
            using (SqlCommand cmd = new SqlCommand(string.Format("UPDATE {0} SET {1} = {2}", table, row, value), this.GetCon()))
            {
                this.TestConnection();
                this.OpenConnectionToDB();

                cmd.ExecuteNonQuery();

                this.CloseConnectionToDB();
            }
        }

        public SqlConnection GetCon()
        {
            return con;
        }

        public string GetConnectionString()
        {
            return con.ConnectionString;
        }
    }
}
