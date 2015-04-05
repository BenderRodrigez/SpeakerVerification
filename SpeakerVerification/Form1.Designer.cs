namespace SpeakerVerification
{
    partial class Form1
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                _graphic2.Dispose();
                _graphic1.Dispose();
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.button3 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.button5 = new System.Windows.Forms.Button();
            this.windowSizeCombo = new System.Windows.Forms.ComboBox();
            this.codeBookCombo = new System.Windows.Forms.ComboBox();
            this.imageLenghtCombo = new System.Windows.Forms.ComboBox();
            this.vectorLenght = new System.Windows.Forms.NumericUpDown();
            this.settingsButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.vectorLenght)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.Highlight;
            this.pictureBox1.Location = new System.Drawing.Point(13, 13);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1024, 256);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.SystemColors.Highlight;
            this.pictureBox2.Location = new System.Drawing.Point(12, 275);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(1024, 256);
            this.pictureBox2.TabIndex = 1;
            this.pictureBox2.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(1043, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(115, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Выбрать файл №1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1043, 41);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(115, 23);
            this.button2.TabIndex = 3;
            this.button2.Text = "Выбрать файл №2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBox1.AutoSize = true;
            this.checkBox1.Enabled = false;
            this.checkBox1.Location = new System.Drawing.Point(1043, 157);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(131, 23);
            this.checkBox1.TabIndex = 4;
            this.checkBox1.Text = "Показать корелляцию";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "Звуковой файл (*.wav) | *.wav";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(1043, 70);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(115, 23);
            this.button3.TabIndex = 5;
            this.button3.Text = "Расчитать график ошибки квантования";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Enabled = false;
            this.label1.Location = new System.Drawing.Point(1040, 96);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Решение:";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(1043, 128);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 7;
            this.button4.Text = "Test";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar1.Location = new System.Drawing.Point(0, 545);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1201, 23);
            this.progressBar1.TabIndex = 8;
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(1043, 187);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(93, 23);
            this.button5.TabIndex = 9;
            this.button5.Text = "Эксперимент";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // windowSizeCombo
            // 
            this.windowSizeCombo.FormattingEnabled = true;
            this.windowSizeCombo.Items.AddRange(new object[] {
            "0,03",
            "0,06",
            "0,09"});
            this.windowSizeCombo.Location = new System.Drawing.Point(1043, 226);
            this.windowSizeCombo.Name = "windowSizeCombo";
            this.windowSizeCombo.Size = new System.Drawing.Size(121, 21);
            this.windowSizeCombo.TabIndex = 10;
            // 
            // codeBookCombo
            // 
            this.codeBookCombo.FormattingEnabled = true;
            this.codeBookCombo.Items.AddRange(new object[] {
            "16",
            "32",
            "64",
            "128",
            "256"});
            this.codeBookCombo.Location = new System.Drawing.Point(1043, 253);
            this.codeBookCombo.Name = "codeBookCombo";
            this.codeBookCombo.Size = new System.Drawing.Size(121, 21);
            this.codeBookCombo.TabIndex = 11;
            // 
            // imageLenghtCombo
            // 
            this.imageLenghtCombo.FormattingEnabled = true;
            this.imageLenghtCombo.Items.AddRange(new object[] {
            "128",
            "256",
            "512",
            "1024",
            "2048"});
            this.imageLenghtCombo.Location = new System.Drawing.Point(1043, 281);
            this.imageLenghtCombo.Name = "imageLenghtCombo";
            this.imageLenghtCombo.Size = new System.Drawing.Size(121, 21);
            this.imageLenghtCombo.TabIndex = 12;
            // 
            // vectorLenght
            // 
            this.vectorLenght.Increment = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.vectorLenght.Location = new System.Drawing.Point(1043, 309);
            this.vectorLenght.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.vectorLenght.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.vectorLenght.Name = "vectorLenght";
            this.vectorLenght.Size = new System.Drawing.Size(120, 20);
            this.vectorLenght.TabIndex = 13;
            this.vectorLenght.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // settingsButton
            // 
            this.settingsButton.Location = new System.Drawing.Point(1043, 336);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(75, 23);
            this.settingsButton.TabIndex = 14;
            this.settingsButton.Text = "Задать";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(1201, 568);
            this.Controls.Add(this.settingsButton);
            this.Controls.Add(this.vectorLenght);
            this.Controls.Add(this.imageLenghtCombo);
            this.Controls.Add(this.codeBookCombo);
            this.Controls.Add(this.windowSizeCombo);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.vectorLenght)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button button5;
        private LinearPredictCoefficient lpc1;
        private LinearPredictCoefficient lpc2;
        private System.Windows.Forms.ComboBox windowSizeCombo;
        private System.Windows.Forms.ComboBox codeBookCombo;
        private System.Windows.Forms.ComboBox imageLenghtCombo;
        private System.Windows.Forms.NumericUpDown vectorLenght;
        private System.Windows.Forms.Button settingsButton;
    }
}

