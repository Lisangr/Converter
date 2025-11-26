
        private void subtitleLabel_Paint(object? sender, PaintEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(currentSubtitleText) || currentSubtitleFont == null || !currentSubtitleForeColor.HasValue)
            {
                return;
            }

            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Determine colors
            var foreColor = currentSubtitleForeColor.Value;
            // For outline, use a semi-transparent black or a color derived from background/theme
            var outlineColor = Color.FromArgb(160, 0, 0, 0); // Semi-transparent black for outline

            // Set background for the label if it has one
            if (currentSubtitleBackColor.HasValue)
            {
                using var backgroundBrush = new SolidBrush(currentSubtitleBackColor.Value);
                g.FillRectangle(backgroundBrush, e.ClipRectangle);
            }

            // Prepare text format flags based on alignment
            TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;
            switch (currentSubtitleAlignment)
            {
                case ContentAlignment.BottomCenter: flags |= TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter; break;
                case ContentAlignment.TopCenter: flags |= TextFormatFlags.Top | TextFormatFlags.HorizontalCenter; break;
                case ContentAlignment.MiddleCenter: flags |= TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter; break;
                // Add other alignments if needed
            }

            // Draw outline if thickness is greater than 0
            if (currentOutlineThickness > 0)
            {
                // Draw outline by drawing text with offsets in outline color
                for (int xOffset = -currentOutlineThickness; xOffset <= currentOutlineThickness; xOffset++)
                {
                    for (int yOffset = -currentOutlineThickness; yOffset <= currentOutlineThickness; yOffset++)
                    {
                        // Avoid drawing outline on the center position if it's the same as foreground
                        if (Math.Abs(xOffset) == currentOutlineThickness && Math.Abs(yOffset) == currentOutlineThickness && currentOutlineThickness > 0) continue; // Skip corners for a cleaner look if desired

                        // Draw the outline text
                        TextRenderer.DrawText(g, currentSubtitleText, currentSubtitleFont, e.ClipRectangle.Add(xOffset, yOffset), outlineColor, flags);
                    }
                }
            }

            // Draw the main text
            TextRenderer.DrawText(g, currentSubtitleText, currentSubtitleFont, e.ClipRectangle, foreColor, flags);
        }
