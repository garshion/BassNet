namespace Test.Client
{
	partial class frmMain
	{
		/// <summary>
		/// 필수 디자이너 변수입니다.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 사용 중인 모든 리소스를 정리합니다.
		/// </summary>
		/// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form 디자이너에서 생성한 코드

		/// <summary>
		/// 디자이너 지원에 필요한 메서드입니다. 
		/// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
		/// </summary>
		private void InitializeComponent()
		{
            this.button1 = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtChat = new System.Windows.Forms.TextBox();
            this.txtNick = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.listChat = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1037, 252);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(189, 58);
            this.button1.TabIndex = 15;
            this.button1.Text = "접속끊기";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(1037, 178);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(186, 68);
            this.btnConnect.TabIndex = 14;
            this.btnConnect.Text = "접속";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(1037, 1064);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(186, 52);
            this.btnSend.TabIndex = 13;
            this.btnSend.Text = "전송";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtChat
            // 
            this.txtChat.Location = new System.Drawing.Point(16, 1064);
            this.txtChat.Name = "txtChat";
            this.txtChat.Size = new System.Drawing.Size(998, 28);
            this.txtChat.TabIndex = 12;
            this.txtChat.TextChanged += new System.EventHandler(this.txtChat_TextChanged);
            // 
            // txtNick
            // 
            this.txtNick.Location = new System.Drawing.Point(1037, 489);
            this.txtNick.Name = "txtNick";
            this.txtNick.Size = new System.Drawing.Size(188, 28);
            this.txtNick.TabIndex = 11;
            this.txtNick.TextChanged += new System.EventHandler(this.txtNick_TextChanged);
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(1037, 531);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(189, 74);
            this.btnLogin.TabIndex = 10;
            this.btnLogin.Text = "로그인";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(1034, 27);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(189, 72);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "지우기";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // listChat
            // 
            this.listChat.FormattingEnabled = true;
            this.listChat.ItemHeight = 18;
            this.listChat.Location = new System.Drawing.Point(16, 16);
            this.listChat.Name = "listChat";
            this.listChat.Size = new System.Drawing.Size(998, 1030);
            this.listChat.TabIndex = 8;
            this.listChat.SelectedIndexChanged += new System.EventHandler(this.listChat_SelectedIndexChanged);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1253, 1131);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtChat);
            this.Controls.Add(this.txtNick);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.listChat);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "frmMain";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button btnConnect;
		private System.Windows.Forms.Button btnSend;
		private System.Windows.Forms.TextBox txtChat;
		private System.Windows.Forms.TextBox txtNick;
		private System.Windows.Forms.Button btnLogin;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.ListBox listChat;
	}
}

