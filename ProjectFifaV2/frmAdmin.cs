using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;

namespace ProjectFifaV2
{
    public partial class frmAdmin : Form
    {
        private DatabaseHandler dbh;
        private OpenFileDialog opfd;

        DataTable table;

        public frmAdmin()
        {
            dbh = new DatabaseHandler();
            table = new DataTable();
            this.ControlBox = false;
            InitializeComponent();
        }

        private void btnAdminLogOut_Click(object sender, EventArgs e)
        {
            txtQuery.Text = null;
            txtPath = null;
            dgvAdminData.DataSource = null;
            Hide();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            if (txtQuery.TextLength > 0)
            {
                ExecuteSQL(txtQuery.Text);
            }
        }

        private void ExecuteSQL(string selectCommandText)
        {
            dbh.TestConnection();
            SqlDataAdapter dataAdapter = new SqlDataAdapter(selectCommandText, dbh.GetCon());

            try
            {
                dataAdapter.Fill(table);
            }
            catch (System.Data.SqlClient.SqlException)
            {
                MessageHandler.ShowMessage("Unknown SQL command", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            
            dgvAdminData.DataSource = table;
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            txtPath.Text = null;
            
            string path = GetFilePath();

            if (CheckExtension(path, "csv"))
            {
                txtPath.Text = path;
            }
            else
            {
                MessageHandler.ShowMessage("The wrong filetype is selected.");
            }
        }

        private void btnLoadData_Click(object sender, EventArgs e)
        {
            if (txtPath.Text != "")
            {
                string[] pathSplit = txtPath.Text.Split('\\');
                int latestIndex = pathSplit.Length - 1;
                StreamReader sr;
                bool success = true;

                string fileName = pathSplit[latestIndex];

                dbh.OpenConnectionToDB();

                try
                {
                    sr = new StreamReader(txtPath.Text);
                }
                catch (System.IO.DirectoryNotFoundException)
                {
                    MessageHandler.ShowMessage(string.Format("Couldn't find the directory."));
                    success = false;
                }
                catch (System.IO.FileNotFoundException)
                {
                    MessageHandler.ShowMessage("Couldn't find the file.");
                    success = false;
                }
                catch (System.ArgumentException)
                {
                    MessageHandler.ShowMessage("Unkown path", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    success = false;
                }

                if (success)
                {
                    sr = new StreamReader(txtPath.Text);

                    string line = sr.ReadLine();
                    string[] value = line.Split(',');

                    DataTable dt = new DataTable();

                    foreach (string dc in value)
                    {
                        dt.Columns.Add(new DataColumn(dc));
                    }

                    while (!sr.EndOfStream)
                    {
                        value = sr.ReadLine().Split(',');
                        if (value.Length == dt.Columns.Count)
                        {
                            DataRow row = dt.NewRow();
                            row.ItemArray = value;
                            dt.Rows.Add(row);
                        }
                        else
                        {
                            MessageHandler.ShowMessage("Amount of columns not consistent");
                            return;
                        }
                    }

                    SqlBulkCopy bc = new SqlBulkCopy(dbh.GetConnectionString(), SqlBulkCopyOptions.TableLock);

                    if (!fileName.Contains("csv"))
                    {
                        MessageHandler.ShowMessage("This isn't a CSV file", "CSV Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (fileName.Contains("teams"))
                    {
                        bc.DestinationTableName = "TblTeams";

                        dbh.TruncateTable("TblTeams");
                        MessageHandler.ShowMessage("Teams toegevoegd");
                    }
                    else if (fileName.Contains("matches"))
                    {
                        bc.DestinationTableName = "TblGames";

                        dbh.TruncateTable("TblGames");
                        MessageHandler.ShowMessage("Matches toegevoegd");
                    }
                    else
                    {
                        MessageHandler.ShowMessage("There was no matching Table found for this csv file", "CSV Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    bc.BatchSize = dt.Rows.Count;
                    bc.WriteToServer(dt);
                    bc.Close();

                }
            }

            dbh.CloseConnectionToDB();

        }

        private string GetFilePath()
        {
            string filePath = "";
            opfd = new OpenFileDialog();

            opfd.Multiselect = false;

            if (opfd.ShowDialog() == DialogResult.OK)
            {
                filePath = opfd.FileName;
            }

            return filePath;
        }

        private bool CheckExtension(string fileString, string extension)
        {
            int extensionLength = extension.Length;
            int strLength = fileString.Length;

            string ext = fileString.Substring(strLength - extensionLength, extensionLength);

            if (ext == extension)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            dbh.EditRow("TblUsers", "Score", "0");

            DataTable playedMatches = dbh.FillDT("SELECT * FROM TblGames WHERE isPlayed = 1");

            if (playedMatches.Rows.Count < 1)
            {
                MessageHandler.ShowMessage("Please upload a csv which contains played matches");
            }
            else
            {
                foreach (DataRow playedMatch in playedMatches.Rows)
                {
                    int gameId = (int)playedMatch["Game_id"];

                    DataTable predictions = dbh.FillDT(string.Format("SELECT * FROM TblPredictions WHERE Game_id = {0}", gameId));

                    int homeTeam = (int)playedMatch["HomeTeam"];
                    int awayTeam = (int)playedMatch["AwayTeam"];
                    int scoreHomeTeam = (int)playedMatch["HomeTeamScore"];
                    int scoreAwayTeam = (int)playedMatch["AwayTeamScore"];
                    int? winningTeam = null;
                    int tie = 0;

                    if (scoreHomeTeam > scoreAwayTeam)
                    {
                        winningTeam = homeTeam;
                    }
                    else if (scoreAwayTeam > scoreHomeTeam)
                    {
                        winningTeam = awayTeam;
                    }
                    else if (scoreHomeTeam == scoreAwayTeam)
                    {
                        tie = 1;
                    }

                    foreach (DataRow prediction in predictions.Rows)
                    {
                        int score = 0;

                        int predictedHomeTeam = (int)prediction["HomeTeam"];
                        int predictedAwayTeam = (int)prediction["AwayTeam"];
                        int predictedHomeTeamScore = (int)prediction["PredictedHomeScore"];
                        int predictedAwayTeamScore = (int)prediction["PredictedAwayScore"];

                        int? predictedWinningTeam = null;

                        if (predictedHomeTeamScore > predictedAwayTeamScore)
                        {
                            predictedWinningTeam = predictedHomeTeam;
                        }
                        else if (predictedAwayTeamScore > predictedHomeTeamScore)
                        {
                            predictedWinningTeam = predictedAwayTeam;
                        }

                        if (predictedHomeTeamScore == scoreHomeTeam && predictedAwayTeamScore == scoreAwayTeam)
                        {
                            score = 3;
                        }
                        else if (winningTeam != null && tie == 0 && predictedWinningTeam != null)
                        {
                            if (winningTeam == predictedWinningTeam)
                            {
                                score = 1;
                            }
                        }

                        MessageHandler.ShowMessage(score.ToString());

                        using (SqlCommand cmd = new SqlCommand("UPDATE TblUsers SET Score = Score + @Score WHERE Id = @Id", dbh.GetCon()))
                        {
                            cmd.Parameters.AddWithValue("Score", score);
                            cmd.Parameters.AddWithValue("Id", prediction["User_id"]);

                            dbh.TestConnection();
                            dbh.OpenConnectionToDB();

                            cmd.ExecuteNonQuery();

                            dbh.CloseConnectionToDB();
                        }
                    }
                }
            }
        }
    }
}
