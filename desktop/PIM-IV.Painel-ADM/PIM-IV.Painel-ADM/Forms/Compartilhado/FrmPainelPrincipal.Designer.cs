
namespace PIM_IV.Painel_ADM
{
    partial class FrmPainelPrincipal
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">
        /// true if managed resources should be disposed;
        /// otherwise, false.
        /// </param>
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
        /// Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();

            // Configura o formulario principal
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 611);
            MinimumSize = new Size(1000, 650);
            Name = "FrmPainelPrincipal";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PIM IV - Painel Administrativo";
            WindowState = FormWindowState.Maximized;

            ResumeLayout(false);
        }

        #endregion
    }
}
