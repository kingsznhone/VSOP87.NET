namespace AsyncTest
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            button1 = new Button();
            timer1 = new System.Windows.Forms.Timer(components);
            label_time = new Label();
            label_version = new Label();
            label_body = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(40, 90);
            label1.Name = "label1";
            label1.Size = new Size(63, 24);
            label1.TabIndex = 0;
            label1.Text = "label1";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(40, 130);
            label2.Name = "label2";
            label2.Size = new Size(63, 24);
            label2.TabIndex = 1;
            label2.Text = "label2";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(40, 170);
            label3.Name = "label3";
            label3.Size = new Size(63, 24);
            label3.TabIndex = 2;
            label3.Text = "label3";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(40, 210);
            label4.Name = "label4";
            label4.Size = new Size(63, 24);
            label4.TabIndex = 3;
            label4.Text = "label4";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(40, 250);
            label5.Name = "label5";
            label5.Size = new Size(63, 24);
            label5.TabIndex = 4;
            label5.Text = "label5";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(40, 290);
            label6.Name = "label6";
            label6.Size = new Size(63, 24);
            label6.TabIndex = 5;
            label6.Text = "label6";
            // 
            // button1
            // 
            button1.Location = new Point(375, 334);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 6;
            button1.Text = "Call Async";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 50;
            timer1.Tick += timer1_Tick;
            // 
            // label_time
            // 
            label_time.AutoSize = true;
            label_time.Location = new Point(40, 339);
            label_time.Name = "label_time";
            label_time.Size = new Size(63, 24);
            label_time.TabIndex = 7;
            label_time.Text = "label7";
            // 
            // label_version
            // 
            label_version.AutoSize = true;
            label_version.Location = new Point(40, 30);
            label_version.Name = "label_version";
            label_version.Size = new Size(63, 24);
            label_version.TabIndex = 8;
            label_version.Text = "label7";
            // 
            // label_body
            // 
            label_body.AutoSize = true;
            label_body.Location = new Point(272, 30);
            label_body.Name = "label_body";
            label_body.Size = new Size(63, 24);
            label_body.TabIndex = 9;
            label_body.Text = "label8";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(499, 402);
            Controls.Add(label_body);
            Controls.Add(label_version);
            Controls.Add(label_time);
            Controls.Add(button1);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            FormClosed += Form1_FormClosed;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Button button1;
        private System.Windows.Forms.Timer timer1;
        private Label label_time;
        private Label label_version;
        private Label label_body;
    }
}