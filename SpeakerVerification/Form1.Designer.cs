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
            this.wavFileOpenDialog = new System.Windows.Forms.OpenFileDialog();
            this.codeBookGroupBox = new System.Windows.Forms.GroupBox();
            this.trainFeaturesGroupBox = new System.Windows.Forms.GroupBox();
            this.featureTestGroupBox = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.acfVectorLengthUpDown = new System.Windows.Forms.NumericUpDown();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.useNeuronNetworkCeckBox = new System.Windows.Forms.CheckBox();
            this.vqSizeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.label18 = new System.Windows.Forms.Label();
            this.testDataFileNameLabel = new System.Windows.Forms.Label();
            this.trainDataFileNameLabel = new System.Windows.Forms.Label();
            this.testDataSelectButton = new System.Windows.Forms.Button();
            this.trainDataSelectButton = new System.Windows.Forms.Button();
            this.histogrammBagsNumberUpDown = new System.Windows.Forms.NumericUpDown();
            this.label17 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.vtcVectorLenghtUpDown = new System.Windows.Forms.NumericUpDown();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.mfccVectorLenghtUpDown = new System.Windows.Forms.NumericUpDown();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.overlappingUpDown = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.arcVectorLenghtUpDown = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.lpcVectorLenghtUpDown = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.featureSelectComboBox = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.windowTypeComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.analysisIntervalUpDown = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.acfVectorLengthUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.vqSizeNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.histogrammBagsNumberUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.vtcVectorLenghtUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.mfccVectorLenghtUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.overlappingUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.arcVectorLenghtUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lpcVectorLenghtUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.analysisIntervalUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // wavFileOpenDialog
            // 
            this.wavFileOpenDialog.Filter = "Звуковой файл (*.wav) | *.wav";
            // 
            // codeBookGroupBox
            // 
            this.codeBookGroupBox.BackColor = System.Drawing.Color.Transparent;
            this.codeBookGroupBox.Location = new System.Drawing.Point(12, 12);
            this.codeBookGroupBox.Name = "codeBookGroupBox";
            this.codeBookGroupBox.Size = new System.Drawing.Size(256, 256);
            this.codeBookGroupBox.TabIndex = 0;
            this.codeBookGroupBox.TabStop = false;
            this.codeBookGroupBox.Text = "Кодовая книга (обуч.)";
            // 
            // trainFeaturesGroupBox
            // 
            this.trainFeaturesGroupBox.BackColor = System.Drawing.Color.Transparent;
            this.trainFeaturesGroupBox.Location = new System.Drawing.Point(274, 12);
            this.trainFeaturesGroupBox.Name = "trainFeaturesGroupBox";
            this.trainFeaturesGroupBox.Size = new System.Drawing.Size(512, 256);
            this.trainFeaturesGroupBox.TabIndex = 1;
            this.trainFeaturesGroupBox.TabStop = false;
            this.trainFeaturesGroupBox.Text = "Матрица признаков (обуч.)";
            // 
            // featureTestGroupBox
            // 
            this.featureTestGroupBox.BackColor = System.Drawing.Color.Transparent;
            this.featureTestGroupBox.Location = new System.Drawing.Point(12, 274);
            this.featureTestGroupBox.Name = "featureTestGroupBox";
            this.featureTestGroupBox.Size = new System.Drawing.Size(512, 256);
            this.featureTestGroupBox.TabIndex = 2;
            this.featureTestGroupBox.TabStop = false;
            this.featureTestGroupBox.Text = "Матрица признаков (тест)";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.acfVectorLengthUpDown);
            this.groupBox4.Controls.Add(this.label20);
            this.groupBox4.Controls.Add(this.label19);
            this.groupBox4.Controls.Add(this.useNeuronNetworkCeckBox);
            this.groupBox4.Controls.Add(this.vqSizeNumericUpDown);
            this.groupBox4.Controls.Add(this.label18);
            this.groupBox4.Controls.Add(this.testDataFileNameLabel);
            this.groupBox4.Controls.Add(this.trainDataFileNameLabel);
            this.groupBox4.Controls.Add(this.testDataSelectButton);
            this.groupBox4.Controls.Add(this.trainDataSelectButton);
            this.groupBox4.Controls.Add(this.histogrammBagsNumberUpDown);
            this.groupBox4.Controls.Add(this.label17);
            this.groupBox4.Controls.Add(this.label16);
            this.groupBox4.Controls.Add(this.vtcVectorLenghtUpDown);
            this.groupBox4.Controls.Add(this.label15);
            this.groupBox4.Controls.Add(this.label14);
            this.groupBox4.Controls.Add(this.mfccVectorLenghtUpDown);
            this.groupBox4.Controls.Add(this.label13);
            this.groupBox4.Controls.Add(this.label12);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.overlappingUpDown);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.arcVectorLenghtUpDown);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.lpcVectorLenghtUpDown);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.featureSelectComboBox);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.label4);
            this.groupBox4.Controls.Add(this.windowTypeComboBox);
            this.groupBox4.Controls.Add(this.label3);
            this.groupBox4.Controls.Add(this.label2);
            this.groupBox4.Controls.Add(this.analysisIntervalUpDown);
            this.groupBox4.Controls.Add(this.label1);
            this.groupBox4.Location = new System.Drawing.Point(792, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(397, 544);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Настройки анализа";
            // 
            // acfVectorLengthUpDown
            // 
            this.acfVectorLengthUpDown.Location = new System.Drawing.Point(264, 242);
            this.acfVectorLengthUpDown.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
            this.acfVectorLengthUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.acfVectorLengthUpDown.Name = "acfVectorLengthUpDown";
            this.acfVectorLengthUpDown.Size = new System.Drawing.Size(41, 20);
            this.acfVectorLengthUpDown.TabIndex = 35;
            this.acfVectorLengthUpDown.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(177, 244);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(81, 13);
            this.label20.TabIndex = 34;
            this.label20.Text = "Дина вектора:";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(164, 222);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(30, 13);
            this.label19.TabIndex = 33;
            this.label19.Text = "ACF:";
            // 
            // useNeuronNetworkCeckBox
            // 
            this.useNeuronNetworkCeckBox.AutoSize = true;
            this.useNeuronNetworkCeckBox.Location = new System.Drawing.Point(29, 175);
            this.useNeuronNetworkCeckBox.Name = "useNeuronNetworkCeckBox";
            this.useNeuronNetworkCeckBox.Size = new System.Drawing.Size(176, 17);
            this.useNeuronNetworkCeckBox.TabIndex = 32;
            this.useNeuronNetworkCeckBox.Text = "Использовать сеть Кохонена";
            this.useNeuronNetworkCeckBox.UseVisualStyleBackColor = true;
            // 
            // vqSizeNumericUpDown
            // 
            this.vqSizeNumericUpDown.Location = new System.Drawing.Point(158, 149);
            this.vqSizeNumericUpDown.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.vqSizeNumericUpDown.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.vqSizeNumericUpDown.Name = "vqSizeNumericUpDown";
            this.vqSizeNumericUpDown.Size = new System.Drawing.Size(53, 20);
            this.vqSizeNumericUpDown.TabIndex = 31;
            this.vqSizeNumericUpDown.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(26, 151);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(126, 13);
            this.label18.TabIndex = 30;
            this.label18.Text = "Размер кодовой книги:";
            // 
            // testDataFileNameLabel
            // 
            this.testDataFileNameLabel.AutoSize = true;
            this.testDataFileNameLabel.Location = new System.Drawing.Point(149, 513);
            this.testDataFileNameLabel.Name = "testDataFileNameLabel";
            this.testDataFileNameLabel.Size = new System.Drawing.Size(0, 13);
            this.testDataFileNameLabel.TabIndex = 29;
            // 
            // trainDataFileNameLabel
            // 
            this.trainDataFileNameLabel.AutoSize = true;
            this.trainDataFileNameLabel.Location = new System.Drawing.Point(149, 484);
            this.trainDataFileNameLabel.Name = "trainDataFileNameLabel";
            this.trainDataFileNameLabel.Size = new System.Drawing.Size(0, 13);
            this.trainDataFileNameLabel.TabIndex = 28;
            // 
            // testDataSelectButton
            // 
            this.testDataSelectButton.Location = new System.Drawing.Point(9, 508);
            this.testDataSelectButton.Name = "testDataSelectButton";
            this.testDataSelectButton.Size = new System.Drawing.Size(137, 23);
            this.testDataSelectButton.TabIndex = 27;
            this.testDataSelectButton.Text = "Выбрать тест. данные";
            this.testDataSelectButton.UseVisualStyleBackColor = true;
            this.testDataSelectButton.Click += new System.EventHandler(this.testDataSelectButton_Click);
            // 
            // trainDataSelectButton
            // 
            this.trainDataSelectButton.Location = new System.Drawing.Point(9, 479);
            this.trainDataSelectButton.Name = "trainDataSelectButton";
            this.trainDataSelectButton.Size = new System.Drawing.Size(137, 23);
            this.trainDataSelectButton.TabIndex = 26;
            this.trainDataSelectButton.Text = "Выбрать обуч. данные";
            this.trainDataSelectButton.UseVisualStyleBackColor = true;
            this.trainDataSelectButton.Click += new System.EventHandler(this.trainDataSelectButton_Click);
            // 
            // histogrammBagsNumberUpDown
            // 
            this.histogrammBagsNumberUpDown.Location = new System.Drawing.Point(211, 444);
            this.histogrammBagsNumberUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.histogrammBagsNumberUpDown.Name = "histogrammBagsNumberUpDown";
            this.histogrammBagsNumberUpDown.Size = new System.Drawing.Size(44, 20);
            this.histogrammBagsNumberUpDown.TabIndex = 25;
            this.histogrammBagsNumberUpDown.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(26, 446);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(179, 13);
            this.label17.TabIndex = 24;
            this.label17.Text = "Кол-во диапазонов гистограммы:";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(6, 422);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(154, 13);
            this.label16.TabIndex = 23;
            this.label16.Text = "Параметры выделения речи:";
            // 
            // vtcVectorLenghtUpDown
            // 
            this.vtcVectorLenghtUpDown.Location = new System.Drawing.Point(113, 390);
            this.vtcVectorLenghtUpDown.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
            this.vtcVectorLenghtUpDown.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.vtcVectorLenghtUpDown.Name = "vtcVectorLenghtUpDown";
            this.vtcVectorLenghtUpDown.Size = new System.Drawing.Size(42, 20);
            this.vtcVectorLenghtUpDown.TabIndex = 22;
            this.vtcVectorLenghtUpDown.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(26, 392);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(87, 13);
            this.label15.TabIndex = 21;
            this.label15.Text = "Длина вектора:";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 370);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(31, 13);
            this.label14.TabIndex = 20;
            this.label14.Text = "VTC:";
            // 
            // mfccVectorLenghtUpDown
            // 
            this.mfccVectorLenghtUpDown.Location = new System.Drawing.Point(113, 340);
            this.mfccVectorLenghtUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.mfccVectorLenghtUpDown.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.mfccVectorLenghtUpDown.Name = "mfccVectorLenghtUpDown";
            this.mfccVectorLenghtUpDown.Size = new System.Drawing.Size(42, 20);
            this.mfccVectorLenghtUpDown.TabIndex = 19;
            this.mfccVectorLenghtUpDown.Value = new decimal(new int[] {
            13,
            0,
            0,
            0});
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(26, 342);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(81, 13);
            this.label13.TabIndex = 18;
            this.label13.Text = "Дина вектора:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 319);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(39, 13);
            this.label12.TabIndex = 17;
            this.label12.Text = "MFCC:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(230, 124);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(15, 13);
            this.label11.TabIndex = 16;
            this.label11.Text = "%";
            // 
            // overlappingUpDown
            // 
            this.overlappingUpDown.Location = new System.Drawing.Point(167, 122);
            this.overlappingUpDown.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.overlappingUpDown.Name = "overlappingUpDown";
            this.overlappingUpDown.Size = new System.Drawing.Size(57, 20);
            this.overlappingUpDown.TabIndex = 15;
            this.overlappingUpDown.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(26, 124);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(135, 13);
            this.label10.TabIndex = 14;
            this.label10.Text = "Перекрытие интервалов:";
            // 
            // arcVectorLenghtUpDown
            // 
            this.arcVectorLenghtUpDown.Location = new System.Drawing.Point(113, 289);
            this.arcVectorLenghtUpDown.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
            this.arcVectorLenghtUpDown.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.arcVectorLenghtUpDown.Name = "arcVectorLenghtUpDown";
            this.arcVectorLenghtUpDown.Size = new System.Drawing.Size(42, 20);
            this.arcVectorLenghtUpDown.TabIndex = 13;
            this.arcVectorLenghtUpDown.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(26, 291);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(87, 13);
            this.label9.TabIndex = 12;
            this.label9.Text = "Длина вектора:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 268);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(32, 13);
            this.label8.TabIndex = 11;
            this.label8.Text = "ARC:";
            // 
            // lpcVectorLenghtUpDown
            // 
            this.lpcVectorLenghtUpDown.Location = new System.Drawing.Point(113, 242);
            this.lpcVectorLenghtUpDown.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.lpcVectorLenghtUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.lpcVectorLenghtUpDown.Name = "lpcVectorLenghtUpDown";
            this.lpcVectorLenghtUpDown.Size = new System.Drawing.Size(42, 20);
            this.lpcVectorLenghtUpDown.TabIndex = 10;
            this.lpcVectorLenghtUpDown.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(26, 244);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(81, 13);
            this.label7.TabIndex = 9;
            this.label7.Text = "Дина вектора:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 222);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(30, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "LPC:";
            // 
            // featureSelectComboBox
            // 
            this.featureSelectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.featureSelectComboBox.FormattingEnabled = true;
            this.featureSelectComboBox.Items.AddRange(new object[] {
            "LPC",
            "MFCC",
            "ARC",
            "VTC",
            "ACF"});
            this.featureSelectComboBox.Location = new System.Drawing.Point(217, 94);
            this.featureSelectComboBox.Name = "featureSelectComboBox";
            this.featureSelectComboBox.Size = new System.Drawing.Size(121, 21);
            this.featureSelectComboBox.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(26, 97);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(185, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Тип вычисляемой характеристики:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(26, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Тип оконной функции:";
            // 
            // windowTypeComboBox
            // 
            this.windowTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.windowTypeComboBox.FormattingEnabled = true;
            this.windowTypeComboBox.Location = new System.Drawing.Point(152, 69);
            this.windowTypeComboBox.Name = "windowTypeComboBox";
            this.windowTypeComboBox.Size = new System.Drawing.Size(121, 21);
            this.windowTypeComboBox.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(196, 45);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(28, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "сек.";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(26, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Интервал анализа:";
            // 
            // analysisIntervalUpDown
            // 
            this.analysisIntervalUpDown.DecimalPlaces = 3;
            this.analysisIntervalUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.analysisIntervalUpDown.Location = new System.Drawing.Point(136, 43);
            this.analysisIntervalUpDown.Name = "analysisIntervalUpDown";
            this.analysisIntervalUpDown.Size = new System.Drawing.Size(54, 20);
            this.analysisIntervalUpDown.TabIndex = 1;
            this.analysisIntervalUpDown.Value = new decimal(new int[] {
            4,
            0,
            0,
            131072});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Общие настройки:";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(0, 546);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(786, 23);
            this.progressBar1.TabIndex = 4;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(530, 274);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(1201, 568);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.featureTestGroupBox);
            this.Controls.Add(this.trainFeaturesGroupBox);
            this.Controls.Add(this.codeBookGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SpeakerVerifiaction";
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.acfVectorLengthUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.vqSizeNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.histogrammBagsNumberUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.vtcVectorLenghtUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.mfccVectorLenghtUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.overlappingUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.arcVectorLenghtUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lpcVectorLenghtUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.analysisIntervalUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog wavFileOpenDialog;
        private System.Windows.Forms.GroupBox codeBookGroupBox;
        private System.Windows.Forms.GroupBox trainFeaturesGroupBox;
        private System.Windows.Forms.GroupBox featureTestGroupBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox windowTypeComboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown analysisIntervalUpDown;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown lpcVectorLenghtUpDown;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox featureSelectComboBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown arcVectorLenghtUpDown;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown mfccVectorLenghtUpDown;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown overlappingUpDown;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.NumericUpDown vtcVectorLenghtUpDown;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.NumericUpDown histogrammBagsNumberUpDown;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label testDataFileNameLabel;
        private System.Windows.Forms.Label trainDataFileNameLabel;
        private System.Windows.Forms.Button testDataSelectButton;
        private System.Windows.Forms.Button trainDataSelectButton;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.NumericUpDown vqSizeNumericUpDown;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.CheckBox useNeuronNetworkCeckBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.NumericUpDown acfVectorLengthUpDown;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label19;
    }
}

