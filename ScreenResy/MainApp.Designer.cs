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
            labelDisplay = new Label();
            comboDisplay = new ComboBox();
            labelResolution = new Label();
            comboResolution = new ComboBox();
            labelRefresh = new Label();
            comboRefresh = new ComboBox();
            labelScale = new Label();
            comboScale = new ComboBox();
            labelCurrent = new Label();
            buttonApply = new Button();
            buttonClose = new Button();
            labelSeparator = new Label();
            labelCommandLine = new Label();
            labelAbout = new Label();
            linkAuthor = new LinkLabel();
            SuspendLayout();
            //
            // labelDisplay
            //
            labelDisplay.AutoSize = true;
            labelDisplay.Location = new Point(12, 15);
            labelDisplay.Name = "labelDisplay";
            labelDisplay.Size = new Size(48, 15);
            labelDisplay.TabIndex = 0;
            labelDisplay.Text = "&Display:";
            //
            // comboDisplay
            //
            comboDisplay.DropDownStyle = ComboBoxStyle.DropDownList;
            comboDisplay.FormattingEnabled = true;
            comboDisplay.Location = new Point(104, 12);
            comboDisplay.Name = "comboDisplay";
            comboDisplay.Size = new Size(244, 23);
            comboDisplay.TabIndex = 1;
            comboDisplay.SelectedIndexChanged += comboDisplay_SelectedIndexChanged;
            //
            // labelResolution
            //
            labelResolution.AutoSize = true;
            labelResolution.Location = new Point(12, 47);
            labelResolution.Name = "labelResolution";
            labelResolution.Size = new Size(66, 15);
            labelResolution.TabIndex = 2;
            labelResolution.Text = "&Resolution:";
            //
            // comboResolution
            //
            comboResolution.DropDownStyle = ComboBoxStyle.DropDownList;
            comboResolution.FormattingEnabled = true;
            comboResolution.Location = new Point(104, 44);
            comboResolution.MaxDropDownItems = 16;
            comboResolution.Name = "comboResolution";
            comboResolution.Size = new Size(244, 23);
            comboResolution.TabIndex = 3;
            comboResolution.SelectedIndexChanged += comboResolution_SelectedIndexChanged;
            //
            // labelRefresh
            //
            labelRefresh.AutoSize = true;
            labelRefresh.Location = new Point(12, 79);
            labelRefresh.Name = "labelRefresh";
            labelRefresh.Size = new Size(73, 15);
            labelRefresh.TabIndex = 4;
            labelRefresh.Text = "Re&fresh rate:";
            //
            // comboRefresh
            //
            comboRefresh.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRefresh.FormattingEnabled = true;
            comboRefresh.Location = new Point(104, 76);
            comboRefresh.Name = "comboRefresh";
            comboRefresh.Size = new Size(244, 23);
            comboRefresh.TabIndex = 5;
            //
            // labelScale
            //
            labelScale.AutoSize = true;
            labelScale.Location = new Point(12, 111);
            labelScale.Name = "labelScale";
            labelScale.Size = new Size(37, 15);
            labelScale.TabIndex = 6;
            labelScale.Text = "&Scale:";
            //
            // comboScale
            //
            comboScale.DropDownStyle = ComboBoxStyle.DropDownList;
            comboScale.FormattingEnabled = true;
            comboScale.Location = new Point(104, 108);
            comboScale.Name = "comboScale";
            comboScale.Size = new Size(244, 23);
            comboScale.TabIndex = 7;
            //
            // labelCurrent
            //
            labelCurrent.ForeColor = SystemColors.GrayText;
            labelCurrent.Location = new Point(12, 142);
            labelCurrent.Name = "labelCurrent";
            labelCurrent.Size = new Size(336, 15);
            labelCurrent.TabIndex = 8;
            labelCurrent.Text = "Current:";
            //
            // buttonApply
            //
            buttonApply.Location = new Point(186, 168);
            buttonApply.Name = "buttonApply";
            buttonApply.Size = new Size(78, 27);
            buttonApply.TabIndex = 9;
            buttonApply.Text = "&Apply";
            buttonApply.UseVisualStyleBackColor = true;
            buttonApply.Click += buttonApply_Click;
            //
            // buttonClose
            //
            buttonClose.DialogResult = DialogResult.Cancel;
            buttonClose.Location = new Point(270, 168);
            buttonClose.Name = "buttonClose";
            buttonClose.Size = new Size(78, 27);
            buttonClose.TabIndex = 10;
            buttonClose.Text = "Close";
            buttonClose.UseVisualStyleBackColor = true;
            buttonClose.Click += buttonClose_Click;
            //
            // labelSeparator
            //
            labelSeparator.BorderStyle = BorderStyle.Fixed3D;
            labelSeparator.Location = new Point(12, 207);
            labelSeparator.Name = "labelSeparator";
            labelSeparator.Size = new Size(336, 2);
            labelSeparator.TabIndex = 11;
            //
            // labelCommandLine
            //
            labelCommandLine.AutoSize = true;
            labelCommandLine.Location = new Point(12, 218);
            labelCommandLine.Name = "labelCommandLine";
            labelCommandLine.Size = new Size(283, 30);
            labelCommandLine.TabIndex = 12;
            labelCommandLine.Text = "Command line:  ScreenResy -x 1920 -y 1080 -d 125\r\nRun  ScreenResy --help  for every option.";
            //
            // labelAbout
            //
            labelAbout.AutoSize = true;
            labelAbout.ForeColor = SystemColors.GrayText;
            labelAbout.Location = new Point(12, 260);
            labelAbout.Name = "labelAbout";
            labelAbout.Size = new Size(63, 15);
            labelAbout.TabIndex = 13;
            labelAbout.Text = "ScreenResy";
            //
            // linkAuthor
            //
            linkAuthor.AutoSize = true;
            linkAuthor.Location = new Point(12, 277);
            linkAuthor.Name = "linkAuthor";
            linkAuthor.Size = new Size(60, 15);
            linkAuthor.TabIndex = 14;
            linkAuthor.TabStop = true;
            linkAuthor.Text = "Joe Farrell";
            linkAuthor.LinkClicked += linkAuthor_LinkClicked;
            //
            // MainApp
            //
            AcceptButton = buttonApply;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonClose;
            ClientSize = new Size(360, 304);
            Controls.Add(linkAuthor);
            Controls.Add(labelAbout);
            Controls.Add(labelCommandLine);
            Controls.Add(labelSeparator);
            Controls.Add(buttonClose);
            Controls.Add(buttonApply);
            Controls.Add(labelCurrent);
            Controls.Add(comboScale);
            Controls.Add(labelScale);
            Controls.Add(comboRefresh);
            Controls.Add(labelRefresh);
            Controls.Add(comboResolution);
            Controls.Add(labelResolution);
            Controls.Add(comboDisplay);
            Controls.Add(labelDisplay);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainApp";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ScreenResy";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelDisplay;
        private ComboBox comboDisplay;
        private Label labelResolution;
        private ComboBox comboResolution;
        private Label labelRefresh;
        private ComboBox comboRefresh;
        private Label labelScale;
        private ComboBox comboScale;
        private Label labelCurrent;
        private Button buttonApply;
        private Button buttonClose;
        private Label labelSeparator;
        private Label labelCommandLine;
        private Label labelAbout;
        private LinkLabel linkAuthor;
    }
}
