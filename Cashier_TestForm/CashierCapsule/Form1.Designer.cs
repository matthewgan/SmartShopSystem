namespace CashierCapsule
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.openControllerBtn = new System.Windows.Forms.Button();
            this.closeControllerBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.sendBtn1 = new System.Windows.Forms.Button();
            this.sendBtn2 = new System.Windows.Forms.Button();
            this.sendBtn3 = new System.Windows.Forms.Button();
            this.sendBtn4 = new System.Windows.Forms.Button();
            this.sendBtn5 = new System.Windows.Forms.Button();
            this.CleanBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(14, 41);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(582, 481);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(14, 12);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(161, 23);
            this.comboBox1.TabIndex = 3;
            // 
            // openControllerBtn
            // 
            this.openControllerBtn.Location = new System.Drawing.Point(181, 12);
            this.openControllerBtn.Name = "openControllerBtn";
            this.openControllerBtn.Size = new System.Drawing.Size(142, 23);
            this.openControllerBtn.TabIndex = 4;
            this.openControllerBtn.Text = "Open Controller";
            this.openControllerBtn.UseVisualStyleBackColor = true;
            this.openControllerBtn.Click += new System.EventHandler(this.controllerOpenBtn_Click);
            // 
            // closeControllerBtn
            // 
            this.closeControllerBtn.Location = new System.Drawing.Point(329, 11);
            this.closeControllerBtn.Name = "closeControllerBtn";
            this.closeControllerBtn.Size = new System.Drawing.Size(161, 23);
            this.closeControllerBtn.TabIndex = 5;
            this.closeControllerBtn.Text = "Close Controller";
            this.closeControllerBtn.UseVisualStyleBackColor = true;
            this.closeControllerBtn.Click += new System.EventHandler(this.controllerCloseBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(602, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(95, 15);
            this.label1.TabIndex = 8;
            this.label1.Text = "PC->Arduino";
            // 
            // sendBtn1
            // 
            this.sendBtn1.Location = new System.Drawing.Point(602, 79);
            this.sendBtn1.Name = "sendBtn1";
            this.sendBtn1.Size = new System.Drawing.Size(153, 47);
            this.sendBtn1.TabIndex = 9;
            this.sendBtn1.Text = "DetectCustomerIn Respond";
            this.sendBtn1.UseVisualStyleBackColor = true;
            this.sendBtn1.Click += new System.EventHandler(this.sendBtn1_Click);
            // 
            // sendBtn2
            // 
            this.sendBtn2.Location = new System.Drawing.Point(602, 132);
            this.sendBtn2.Name = "sendBtn2";
            this.sendBtn2.Size = new System.Drawing.Size(153, 47);
            this.sendBtn2.TabIndex = 10;
            this.sendBtn2.Text = "DoorClosed Respond";
            this.sendBtn2.UseVisualStyleBackColor = true;
            this.sendBtn2.Click += new System.EventHandler(this.sendBtn2_Click);
            // 
            // sendBtn3
            // 
            this.sendBtn3.Location = new System.Drawing.Point(602, 185);
            this.sendBtn3.Name = "sendBtn3";
            this.sendBtn3.Size = new System.Drawing.Size(153, 47);
            this.sendBtn3.TabIndex = 11;
            this.sendBtn3.Text = "OpenDoor  Inwards";
            this.sendBtn3.UseVisualStyleBackColor = true;
            this.sendBtn3.Click += new System.EventHandler(this.sendBtn3_Click);
            // 
            // sendBtn4
            // 
            this.sendBtn4.Location = new System.Drawing.Point(602, 238);
            this.sendBtn4.Name = "sendBtn4";
            this.sendBtn4.Size = new System.Drawing.Size(153, 47);
            this.sendBtn4.TabIndex = 12;
            this.sendBtn4.Text = "OpenDoor Outwards";
            this.sendBtn4.UseVisualStyleBackColor = true;
            this.sendBtn4.Click += new System.EventHandler(this.sendBtn4_Click);
            // 
            // sendBtn5
            // 
            this.sendBtn5.Location = new System.Drawing.Point(602, 291);
            this.sendBtn5.Name = "sendBtn5";
            this.sendBtn5.Size = new System.Drawing.Size(153, 47);
            this.sendBtn5.TabIndex = 13;
            this.sendBtn5.Text = "DetectCustomerOut Respond";
            this.sendBtn5.UseVisualStyleBackColor = true;
            this.sendBtn5.Click += new System.EventHandler(this.sendBtn5_Click);
            // 
            // CleanBtn
            // 
            this.CleanBtn.Location = new System.Drawing.Point(605, 475);
            this.CleanBtn.Name = "CleanBtn";
            this.CleanBtn.Size = new System.Drawing.Size(153, 47);
            this.CleanBtn.TabIndex = 14;
            this.CleanBtn.Text = "Clean Msgbox";
            this.CleanBtn.UseVisualStyleBackColor = true;
            this.CleanBtn.Click += new System.EventHandler(this.CleanBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(760, 536);
            this.Controls.Add(this.CleanBtn);
            this.Controls.Add(this.sendBtn5);
            this.Controls.Add(this.sendBtn4);
            this.Controls.Add(this.sendBtn3);
            this.Controls.Add(this.sendBtn2);
            this.Controls.Add(this.sendBtn1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.closeControllerBtn);
            this.Controls.Add(this.openControllerBtn);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.richTextBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button openControllerBtn;
        private System.Windows.Forms.Button closeControllerBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button sendBtn1;
        private System.Windows.Forms.Button sendBtn2;
        private System.Windows.Forms.Button sendBtn3;
        private System.Windows.Forms.Button sendBtn4;
        private System.Windows.Forms.Button sendBtn5;
        private System.Windows.Forms.Button CleanBtn;
    }
}

