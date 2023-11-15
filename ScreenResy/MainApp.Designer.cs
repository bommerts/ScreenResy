namespace ScreenResy
{
    partial class MainApp
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
            buttonChangeResolution = new Button();
            textBoxXRes = new TextBox();
            textBoxYRes = new TextBox();
            labelX1 = new Label();
            textBoxScale = new TextBox();
            buttonChangeScale = new Button();
            labelScalePercent = new Label();
            labelAppName = new Label();
            labelVersion = new Label();
            labelAuthor = new Label();
            labelCommandLineArgs = new Label();
            buttonExit = new Button();
            labelAuthorURL = new Label();
            SuspendLayout();
            // 
            // buttonChangeResolution
            // 
            buttonChangeResolution.Location = new Point(11, 142);
            buttonChangeResolution.Name = "buttonChangeResolution";
            buttonChangeResolution.Size = new Size(186, 50);
            buttonChangeResolution.TabIndex = 0;
            buttonChangeResolution.Text = "Change Resolution";
            buttonChangeResolution.UseVisualStyleBackColor = true;
            buttonChangeResolution.Click += buttonChangeResolution_Click;
            // 
            // textBoxXRes
            // 
            textBoxXRes.Location = new Point(11, 97);
            textBoxXRes.Name = "textBoxXRes";
            textBoxXRes.Size = new Size(78, 31);
            textBoxXRes.TabIndex = 1;
            textBoxXRes.Text = "1920";
            // 
            // textBoxYRes
            // 
            textBoxYRes.Location = new Point(117, 97);
            textBoxYRes.Name = "textBoxYRes";
            textBoxYRes.Size = new Size(78, 31);
            textBoxYRes.TabIndex = 2;
            textBoxYRes.Text = "1080";
            // 
            // labelX1
            // 
            labelX1.AutoSize = true;
            labelX1.Location = new Point(91, 100);
            labelX1.Name = "labelX1";
            labelX1.Size = new Size(20, 25);
            labelX1.TabIndex = 3;
            labelX1.Text = "x";
            // 
            // textBoxScale
            // 
            textBoxScale.Location = new Point(263, 97);
            textBoxScale.Name = "textBoxScale";
            textBoxScale.Size = new Size(78, 31);
            textBoxScale.TabIndex = 4;
            textBoxScale.Text = "100";
            // 
            // buttonChangeScale
            // 
            buttonChangeScale.Location = new Point(263, 142);
            buttonChangeScale.Name = "buttonChangeScale";
            buttonChangeScale.Size = new Size(179, 50);
            buttonChangeScale.TabIndex = 5;
            buttonChangeScale.Text = "Change DPI Scale";
            buttonChangeScale.UseVisualStyleBackColor = true;
            buttonChangeScale.Click += buttonChangeScale_Click;
            // 
            // labelScalePercent
            // 
            labelScalePercent.AutoSize = true;
            labelScalePercent.Location = new Point(347, 100);
            labelScalePercent.Name = "labelScalePercent";
            labelScalePercent.Size = new Size(27, 25);
            labelScalePercent.TabIndex = 6;
            labelScalePercent.Text = "%";
            // 
            // labelAppName
            // 
            labelAppName.AutoSize = true;
            labelAppName.Location = new Point(11, 8);
            labelAppName.Name = "labelAppName";
            labelAppName.Size = new Size(100, 25);
            labelAppName.TabIndex = 7;
            labelAppName.Text = "ScreenResy";
            // 
            // labelVersion
            // 
            labelVersion.AutoSize = true;
            labelVersion.Location = new Point(108, 8);
            labelVersion.Name = "labelVersion";
            labelVersion.Size = new Size(108, 25);
            labelVersion.TabIndex = 8;
            labelVersion.Text = "version 1.01";
            // 
            // labelAuthor
            // 
            labelAuthor.AutoSize = true;
            labelAuthor.Location = new Point(11, 33);
            labelAuthor.Name = "labelAuthor";
            labelAuthor.Size = new Size(269, 25);
            labelAuthor.TabIndex = 9;
            labelAuthor.Text = "Copyright (C) 2023 by Joe Farrell";
            // 
            // labelCommandLineArgs
            // 
            labelCommandLineArgs.AutoSize = true;
            labelCommandLineArgs.Location = new Point(11, 203);
            labelCommandLineArgs.Name = "labelCommandLineArgs";
            labelCommandLineArgs.Size = new Size(341, 175);
            labelCommandLineArgs.TabIndex = 10;
            labelCommandLineArgs.Text = "Command Line Usage:\r\n\r\n-x (pixel value) -y (pixel value) -d (% value)\r\n\r\nExample:\r\n\r\n-x 1920 -y 1080 -d 125";
            // 
            // buttonExit
            // 
            buttonExit.Location = new Point(331, 331);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(111, 47);
            buttonExit.TabIndex = 11;
            buttonExit.Text = "Exit";
            buttonExit.UseVisualStyleBackColor = true;
            buttonExit.Click += buttonExit_Click;
            // 
            // labelAuthorURL
            // 
            labelAuthorURL.AutoSize = true;
            labelAuthorURL.Location = new Point(12, 58);
            labelAuthorURL.Name = "labelAuthorURL";
            labelAuthorURL.Size = new Size(317, 25);
            labelAuthorURL.TabIndex = 12;
            labelAuthorURL.Text = "https://www.linkedin.com/in/joefarrell/";
            // 
            // MainApp
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(454, 393);
            Controls.Add(labelAuthorURL);
            Controls.Add(buttonExit);
            Controls.Add(labelCommandLineArgs);
            Controls.Add(labelAuthor);
            Controls.Add(labelVersion);
            Controls.Add(labelAppName);
            Controls.Add(labelScalePercent);
            Controls.Add(buttonChangeScale);
            Controls.Add(textBoxScale);
            Controls.Add(labelX1);
            Controls.Add(textBoxYRes);
            Controls.Add(textBoxXRes);
            Controls.Add(buttonChangeResolution);
            Name = "MainApp";
            Text = "ScreenResy";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonChangeResolution;
        private TextBox textBoxXRes;
        private TextBox textBoxYRes;
        private Label labelX1;
        private TextBox textBoxScale;
        private Button buttonChangeScale;
        private Label labelScalePercent;
        private Label labelAppName;
        private Label labelVersion;
        private Label labelAuthor;
        private Label labelCommandLineArgs;
        private Button buttonExit;
        private Label labelAuthorURL;
    }
}