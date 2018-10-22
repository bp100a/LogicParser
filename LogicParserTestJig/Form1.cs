using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LogicParser;

namespace LogicParserTestJig
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.label1.Text = "Working...";
            this.label1.Update();

            // Okay the work goes here!
            int j = 0;
            for (int i = 0; i < 500000; i++)
            {
                status s = LogicExpressionEvaluator.Evaluate("1 && 1 || 0");
//                LogicExpressionEvaluator.Evaluate("1 && (0 || 1) && 5 > 2");
                j++;
                if (j == 1000)
                {
                    this.output.Text = (i+1).ToString();
                    this.output.Update();
                    j = 0;
                }

            }
            this.label1.Text = "Done!";
            this.label1.Update();

        }
    }
}
