namespace SP.Visualizer.Conector
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cbServers = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.cbAuthMode = new System.Windows.Forms.ComboBox();
            this.btnFinish = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbUser = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbServers
            // 
            this.cbServers.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbServers.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbServers.FormattingEnabled = true;
            this.cbServers.Location = new System.Drawing.Point(90, 12);
            this.cbServers.Name = "cbServers";
            this.cbServers.Size = new System.Drawing.Size(124, 23);
            this.cbServers.TabIndex = 0;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(220, 12);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Atualizar";
            this.btnRefresh.UseVisualStyleBackColor = true;
            // 
            // cbAuthMode
            // 
            this.cbAuthMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAuthMode.FormattingEnabled = true;
            this.cbAuthMode.Location = new System.Drawing.Point(90, 41);
            this.cbAuthMode.Name = "cbAuthMode";
            this.cbAuthMode.Size = new System.Drawing.Size(205, 23);
            this.cbAuthMode.TabIndex = 2;
            this.cbAuthMode.SelectionChangeCommitted += new System.EventHandler(this.cbAuthMode_SelectionChangeCommitted);
            // 
            // btnFinish
            // 
            this.btnFinish.Location = new System.Drawing.Point(220, 143);
            this.btnFinish.Name = "btnFinish";
            this.btnFinish.Size = new System.Drawing.Size(75, 23);
            this.btnFinish.TabIndex = 3;
            this.btnFinish.Text = "Finalizar";
            this.btnFinish.UseVisualStyleBackColor = true;
            this.btnFinish.Click += new System.EventHandler(this.btnFinish_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "Servidor:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 15);
            this.label2.TabIndex = 5;
            this.label2.Text = "Autenticação:";
            // 
            // tbUser
            // 
            this.tbUser.Enabled = false;
            this.tbUser.Location = new System.Drawing.Point(90, 70);
            this.tbUser.Name = "tbUser";
            this.tbUser.Size = new System.Drawing.Size(205, 23);
            this.tbUser.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 15);
            this.label3.TabIndex = 7;
            this.label3.Text = "Usuário:";
            // 
            // tbPassword
            // 
            this.tbPassword.Enabled = false;
            this.tbPassword.Location = new System.Drawing.Point(90, 99);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.Size = new System.Drawing.Size(205, 23);
            this.tbPassword.TabIndex = 8;
            this.tbPassword.UseSystemPasswordChar = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 15);
            this.label4.TabIndex = 9;
            this.label4.Text = "Senha:";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(308, 176);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbPassword);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbUser);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnFinish);
            this.Controls.Add(this.cbAuthMode);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.cbServers);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.Text = "Selecionar Servidor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ComboBox cbServers;
        private Button btnRefresh;
        private ComboBox cbAuthMode;
        private Button btnFinish;
        private Label label1;
        private Label label2;
        private TextBox tbUser;
        private Label label3;
        private TextBox tbPassword;
        private Label label4;
    }
}