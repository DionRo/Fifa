using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ProjectFifaV2
{
    public partial class frmPlayer : Form
    {
        private Form frmRanking;
        private DatabaseHandler dbh;
        private string userName;

        private DataTable userTable;
        private string[] user;
        private TextBox[,] predictions;
        private int[] matchId;

        List<TextBox> txtBoxList;

        public frmPlayer(Form frm, string un)
        {
            this.ControlBox = false;
            frmRanking = frm;
            dbh = new DatabaseHandler();

            string query = String.Format("SELECT * FROM TblUsers WHERE Username = '{0}'", un);
            userTable = dbh.FillDT(query);

            foreach (DataRow row in userTable.Rows)
            {
                user = new string[] {
                    row["Id"].ToString(),
                    row["Username"].ToString(),
                    row["IsAdmin"].ToString(),
                };
            }

            InitializeComponent();
            if (DisableEditButton())
            {
                btnEditPrediction.Enabled = false;
            }
            ShowResults();
            ShowScoreCard();
            this.Text = "Welcome " + un;

            
        }

        private void btnLogOut_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void btnShowRanking_Click(object sender, EventArgs e)
        {
            frmRanking.Show();
        }

        private void btnClearPrediction_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to clear your prediction?", "Clear Predictions", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result.Equals(DialogResult.OK))
            {
                // Clear predections
                int user_id = int.Parse(user[0]);

                string query = String.Format("DELETE FROM TblPredictions WHERE User_id = '{0}'", user[0]);
                using (SqlCommand cmd = new SqlCommand(query, dbh.GetCon()))
                {
                    dbh.TestConnection();

                    dbh.OpenConnectionToDB();
                    int tblPredictionResult = cmd.ExecuteNonQuery();
                    dbh.CloseConnectionToDB();

                    if (tblPredictionResult > 0)
                    {
                        MessageHandler.ShowMessage(String.Format("You have deleted {0} results.", tblPredictionResult), "Result", MessageBoxButtons.OK, MessageBoxIcon.None);
                    }
                    else
                    {
                        MessageHandler.ShowMessage(String.Format("You didn't delete any rows."), "Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // Update DB
            }
        }

        private bool DisableEditButton()
        {
            bool hasPassed;
            //This is the deadline for filling in the predictions
            DateTime deadline = new DateTime(2018, 06, 12);
            DateTime curTime = DateTime.Now;
            int result = DateTime.Compare(deadline, curTime);

            if (result < 0)
            {
                hasPassed = true;
            }
            else
            {
                hasPassed = false;
            }

            return hasPassed;
        }

        // Dit is de rechtse kolom

        private void ShowResults()
        {
            dbh.TestConnection();
            dbh.OpenConnectionToDB();

            DataTable hometable = dbh.FillDT("SELECT tblTeams.TeamName, tblGames.HomeTeamScore FROM tblGames INNER JOIN tblTeams ON tblGames.HomeTeam = tblTeams.Team_ID ORDER BY TblGames.Game_id");
            DataTable awayTable = dbh.FillDT("SELECT tblTeams.TeamName, tblGames.AwayTeamScore FROM tblGames INNER JOIN tblTeams ON tblGames.AwayTeam = tblTeams.Team_ID ORDER BY TblGames.Game_id");

            dbh.CloseConnectionToDB();

            for (int i = 0; i < hometable.Rows.Count; i++)
            {
                DataRow dataRowHome = hometable.Rows[i];
                DataRow dataRowAway = awayTable.Rows[i];

                ListViewItem lstItem = new ListViewItem(dataRowHome["TeamName"].ToString());
                lstItem.SubItems.Add(dataRowHome["HomeTeamScore"].ToString());
                lstItem.SubItems.Add(dataRowAway["AwayTeamScore"].ToString());
                lstItem.SubItems.Add(dataRowAway["TeamName"].ToString());

                lvOverview.Items.Add(lstItem);
            }
        }


        // Dit is de linkste kolom
        
        private void ShowScoreCard()
        {
            dbh.TestConnection();
            dbh.OpenConnectionToDB();

            DataTable hometable         = dbh.FillDT("SELECT tblGames.Game_id, tblTeams.TeamName FROM tblGames INNER JOIN tblTeams ON tblGames.HomeTeam = tblTeams.Team_ID ORDER BY TblGames.Game_id");
            DataTable awayTable         = dbh.FillDT("SELECT tblGames.Game_id, tblTeams.TeamName FROM tblGames INNER JOIN tblTeams ON tblGames.AwayTeam = tblTeams.Team_ID ORDER BY TblGames.Game_id");
            DataTable oldPredictions    = dbh.FillDT(String.Format("SELECT PredictedHomeScore, PredictedAwayScore FROM TblPredictions WHERE User_id = {0}", int.Parse(user[0])));
            DataTable games             = dbh.FillDT("SELECT Game_id FROM TblGames WHERE isPlayed = 1");

            dbh.CloseConnectionToDB();
            txtBoxList = new List<TextBox>();

            predictions = new TextBox[hometable.Rows.Count, 2];
            matchId = new int[hometable.Rows.Count];

            for (int i = 0; i < hometable.Rows.Count; i++)
            {
                DataRow dataRowHome = hometable.Rows[i];
                DataRow dataRowAway = awayTable.Rows[i];
                DataRow dataRowPredictions;

                bool isPlayed = false;

                matchId[i] = int.Parse(dataRowHome["Game_id"].ToString());

                foreach (DataRow game in games.Rows)
                {
                    if (game["Game_id"].ToString() == matchId[i].ToString())
                    {
                        isPlayed = true;
                    }
                }
                
                Label lblHomeTeam = new Label();
                Label lblAwayTeam = new Label();

                TextBox txtHomePred = new TextBox();
                TextBox txtAwayPred = new TextBox();

                txtBoxList.Add(txtHomePred);
                txtBoxList.Add(txtAwayPred);

                if (oldPredictions.Rows.Count > 0)
                {
                    dataRowPredictions = oldPredictions.Rows[i];

                    txtHomePred.Text = dataRowPredictions["PredictedHomeScore"].ToString();
                    txtAwayPred.Text = dataRowPredictions["PredictedAwayScore"].ToString();
                }
                else
                {
                    txtHomePred.Text = "0";
                    txtAwayPred.Text = "0";
                }

                lblHomeTeam.TextAlign = ContentAlignment.BottomRight;
                lblHomeTeam.Text = dataRowHome["TeamName"].ToString();
                lblHomeTeam.Location = new Point(15, txtHomePred.Bottom + (i * 30));
                lblHomeTeam.AutoSize = true;

                txtHomePred.Location = new Point(lblHomeTeam.Width, lblHomeTeam.Top - 3);
                txtHomePred.Width = 40;
                txtHomePred.Tag = dataRowHome["Game_id"].ToString();
                predictions[i, 0] = txtHomePred;

                txtAwayPred.Location = new Point(txtHomePred.Width + lblHomeTeam.Width, txtHomePred.Top);
                txtAwayPred.Width = 40;
                txtAwayPred.Tag = dataRowAway["Game_id"].ToString();

                lblAwayTeam.Text = dataRowAway["TeamName"].ToString();
                lblAwayTeam.Location = new Point(txtHomePred.Width + lblHomeTeam.Width + txtAwayPred.Width, txtHomePred.Top + 3);
                lblAwayTeam.AutoSize = true;
                predictions[i, 1] = txtAwayPred;

                pnlPredCard.Controls.Add(lblHomeTeam);
                pnlPredCard.Controls.Add(txtHomePred);
                pnlPredCard.Controls.Add(txtAwayPred);
                pnlPredCard.Controls.Add(lblAwayTeam);

                if (isPlayed)
                {
                    txtAwayPred.Enabled = false;
                    txtHomePred.Enabled = false;
                }
                

                //ListViewItem lstItem = new ListViewItem(dataRowHome["TeamName"].ToString());
                //lstItem.SubItems.Add(dataRowHome["HomeTeamScore"].ToString());
                //lstItem.SubItems.Add(dataRowAway["AwayTeamScore"].ToString());
                //lstItem.SubItems.Add(dataRowAway["TeamName"].ToString());
                //lvOverview.Items.Add(lstItem);
            }
        }
        private void btnEditPrediction_Click(object sender, EventArgs e)
        {
            DataTable games             = dbh.FillDT("SELECT * FROM TblGames");
            DataTable savedPredictions  = dbh.FillDT(string.Format("SELECT * FROM TblPredictions WHERE User_id = '{0}'", user[0]));

            for (int i = 0; i < games.Rows.Count; i++)
            {
                if (savedPredictions.Rows.Count == 0)
                {
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO TblPredictions (User_id, Game_id, HomeTeam, AWayTeam, PredictedHomeScore, PredictedAwayScore) VALUES (@User_id, @Game_id, @HomeTeam, @AwayTeam, @PredictedHomeScore, @PredictedAwayScore)", dbh.GetCon()))
                    {
                        cmd.Parameters.AddWithValue("User_id",              user[0]);
                        cmd.Parameters.AddWithValue("Game_id",              matchId[i]);
                        cmd.Parameters.AddWithValue("HomeTeam",             (int)games.Rows[i]["HomeTeam"]);
                        cmd.Parameters.AddWithValue("AwayTeam",             (int)games.Rows[i]["AwayTeam"]);
                        cmd.Parameters.AddWithValue("PredictedHomeScore",   predictions[i, 0].Text);
                        cmd.Parameters.AddWithValue("PredictedAwayScore",   predictions[i, 1].Text);

                        //cmd.Parameters.AddWithValue("TeamHomeId", games.Rows[i]["HomeTeam"])

                        int homeTeam = (int)games.Rows[i]["HomeTeam"];

                        dbh.TestConnection();
                        dbh.OpenConnectionToDB();

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (System.Data.SqlClient.SqlException)
                        {
                            MessageHandler.ShowMessage("Something went wrong", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        dbh.CloseConnectionToDB();
                    }
                }
                else
                {
                    using (SqlCommand cmd = new SqlCommand("UPDATE TblPredictions SET HomeTeam = @HomeTeam, AwayTeam = @AwayTeam, PredictedHomeScore = @PredictedHomeScore, PredictedAwayScore = @PredictedAwayScore WHERE User_id = @User_id AND Game_id = @Game_id", dbh.GetCon()))
                    {
                        cmd.Parameters.AddWithValue("HomeTeam",             (int)games.Rows[i]["HomeTeam"]);
                        cmd.Parameters.AddWithValue("AwayTeam",             (int)games.Rows[i]["AwayTeam"]);
                        cmd.Parameters.AddWithValue("@PredictedHomeScore",  predictions[i, 0].Text);
                        cmd.Parameters.AddWithValue("@PredictedAwayScore",  predictions[i, 1].Text);
                        cmd.Parameters.AddWithValue("@User_id",             user[0]);
                        cmd.Parameters.AddWithValue("@Game_id",             matchId[i]);

                        dbh.TestConnection();
                        dbh.OpenConnectionToDB();

                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (System.Data.SqlClient.SqlException)
                        {
                            MessageHandler.ShowMessage("Something went wrong", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        dbh.CloseConnectionToDB();
                    }
                }

            }

            MessageHandler.ShowMessage("Predictions updated", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        internal void GetUsername(string un)
        {
            userName = un;
        }        
    }
}
