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

            InitializeComponent();
            if (DisableEditButton())
            {
                btnEditPrediction.Enabled = false;
            }
            ShowResults();
            ShowScoreCard();
            this.Text = "Welcome " + un;

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

            DataTable hometable = dbh.FillDT("SELECT tblGames.Game_id, tblTeams.TeamName FROM tblGames INNER JOIN tblTeams ON tblGames.HomeTeam = tblTeams.Team_ID ORDER BY TblGames.Game_id");
            DataTable awayTable = dbh.FillDT("SELECT tblGames.Game_id, tblTeams.TeamName FROM tblGames INNER JOIN tblTeams ON tblGames.AwayTeam = tblTeams.Team_ID ORDER BY TblGames.Game_id");

            dbh.CloseConnectionToDB();
            txtBoxList = new List<TextBox>();

            predictions = new TextBox[hometable.Rows.Count, 2];
            matchId = new int[hometable.Rows.Count];

            for (int i = 0; i < hometable.Rows.Count; i++)
            {
                DataRow dataRowHome = hometable.Rows[i];
                DataRow dataRowAway = awayTable.Rows[i];

                Label lblHomeTeam = new Label();
                Label lblAwayTeam = new Label();

                TextBox txtHomePred = new TextBox();
                TextBox txtAwayPred = new TextBox();

                txtBoxList.Add(txtHomePred);
                txtBoxList.Add(txtAwayPred);

                lblHomeTeam.TextAlign = ContentAlignment.BottomRight;
                lblHomeTeam.Text = dataRowHome["TeamName"].ToString();
                lblHomeTeam.Location = new Point(15, txtHomePred.Bottom + (i * 30));
                lblHomeTeam.AutoSize = true;

                txtHomePred.Text = "1";
                txtHomePred.Location = new Point(lblHomeTeam.Width, lblHomeTeam.Top - 3);
                txtHomePred.Width = 40;
                txtHomePred.Tag = dataRowHome["Game_id"].ToString();
                predictions[i, 0] = txtHomePred;

                txtAwayPred.Text = "1";
                txtAwayPred.Location = new Point(txtHomePred.Width + lblHomeTeam.Width, txtHomePred.Top);
                txtAwayPred.Width = 40;
                txtAwayPred.Tag = dataRowAway["Game_id"].ToString();

                lblAwayTeam.Text = dataRowAway["TeamName"].ToString();
                lblAwayTeam.Location = new Point(txtHomePred.Width + lblHomeTeam.Width + txtAwayPred.Width, txtHomePred.Top + 3);
                lblAwayTeam.AutoSize = true;
                predictions[i, 1] = txtAwayPred;

                matchId[i] = int.Parse(dataRowHome["Game_id"].ToString());

                pnlPredCard.Controls.Add(lblHomeTeam);
                pnlPredCard.Controls.Add(txtHomePred);
                pnlPredCard.Controls.Add(txtAwayPred);
                pnlPredCard.Controls.Add(lblAwayTeam);

                //ListViewItem lstItem = new ListViewItem(dataRowHome["TeamName"].ToString());
                //lstItem.SubItems.Add(dataRowHome["HomeTeamScore"].ToString());
                //lstItem.SubItems.Add(dataRowAway["AwayTeamScore"].ToString());
                //lstItem.SubItems.Add(dataRowAway["TeamName"].ToString());
                //lvOverview.Items.Add(lstItem);
            }
        }
        private void btnEditPrediction_Click(object sender, EventArgs e)
        {
            DataTable games = dbh.FillDT("SELECT * FROM TblGames");

            for (int i = 0; i < games.Rows.Count; i++)
            {
                using (SqlCommand cmd = new SqlCommand("INSERT INTO TblPredictions (User_id, Game_id, PredictedHomeScore, PredictedAwayScore) VALUES (@User_id, @Game_id, @PredictedHomeScore, @PredictedAwayScore)", dbh.GetCon()))
                {
                    cmd.Parameters.AddWithValue("User_id", user[0]);
                    cmd.Parameters.AddWithValue("Game_id", matchId[i]);
                    cmd.Parameters.AddWithValue("PredictedHomeScore", predictions[i, 0].Text);
                    cmd.Parameters.AddWithValue("PredictedAwayScore", predictions[i, 1].Text);

                    dbh.TestConnection();
                    dbh.OpenConnectionToDB();

                    cmd.ExecuteNonQuery();

                    dbh.CloseConnectionToDB();
                }
            }


        }

        internal void GetUsername(string un)
        {
            userName = un;
        }        
    }
}
