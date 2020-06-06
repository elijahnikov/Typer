using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using OpenTK.Graphics.ES10;
using System.Security.Policy;

namespace Typer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {  
            InitializeComponent();
            textFieldArr = Properties.Resources.EnglishText.Split(" ");
            SetTextBox();
            entryBox.Focus();

            if (!File.Exists(themePath))
            {
                File.Create(themePath).Close();
            }

            if (new FileInfo(themePath).Length != 0)
            {
                //applies saved theme on startup
                string line = File.ReadLines(themePath).First();
                switch (line)
                {
                    case "Olivia":
                        Olivia();
                        break;
                    case "Dracula":
                        Dracula();
                        break;
                    case "Dolche":
                        Dolche();
                        break;
                    case "Ashes":
                        Ashes();
                        break;
                    case "":
                        break;
                }
            }
        }
        public void Refresh()
        {
            tempText = "";
            entryBox.IsEnabled = true;
            entryBox.Text = String.Empty;
            entryBox.Focus();
            SetGetText.SetText(mainText, "");
            SetTextBox();

            //reset current typing records
            correctWords = 0;
            accuracy = 0;
            currentWordIndex = 0;
            wpm = 0;

            //empty labels
            accNum.Content = String.Empty;
            wpmNum.Content = String.Empty;
        }

        private void SetTextBox()
        {
            Random rnd = new Random();

            try
            {
                for (int i = 0; i < 50; i++)
                {
                    //generate random index from 0-1000
                    int r = rnd.Next(textFieldArr.Length);

                    //if temptext does not already contain word, add to temptext
                    if (!tempText.Contains(textFieldArr[r])) tempText += textFieldArr[r] + " ";
    
                }
                SetGetText.SetText(mainText, tempText);
            }
            catch (IndexOutOfRangeException ex)
            {
                Debug.WriteLine("{0} Exception caught.", ex);
            }

            String getMainText = SetGetText.GetText(mainText);
            checkInputArr = getMainText.Split(" ");  
        }

        private void RfrshBtn_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        //method to color specific text in richtextbox
        public static void FromTextPointer(TextPointer startPointer, TextPointer endPointer, string keyword, String foreground)
        {
            if (startPointer == null) throw new ArgumentNullException(nameof(startPointer));
            if (endPointer == null) throw new ArgumentNullException(nameof(endPointer));
            if (string.IsNullOrEmpty(keyword)) throw new ArgumentNullException(keyword);

            List<string> list = new List<string>();

            if (!list.Contains(keyword))
            {
                TextRange text = new TextRange(startPointer, endPointer);
                TextPointer current = text.Start.GetInsertionPosition(LogicalDirection.Forward);
                while (current != null)
                {
                    string textInRun = current.GetTextInRun(LogicalDirection.Forward);
                    if (!string.IsNullOrWhiteSpace(textInRun))
                    {
                        int index = textInRun.IndexOf(keyword);
                        if (index != -1)
                        {
                            TextPointer selectionStart = current.GetPositionAtOffset(index, LogicalDirection.Forward);
                            TextPointer selectionEnd = selectionStart.GetPositionAtOffset(keyword.Length, LogicalDirection.Forward);
                            TextRange selection = new TextRange(selectionStart, selectionEnd);
                            BrushConverter bc = new BrushConverter();
                            Brush brush = (Brush)bc.ConvertFrom(foreground);
                            selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                        }
                    }
                    current = current.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            list.Add(keyword);
        }

        //method to append current theme to text file, used to save theme on close and reopen
        private void appendTheme(String path, String text)
        {
            var allLines = File.ReadAllLines(path);

            if (allLines.Length > 0)
            {
                try
                {
                    allLines[0] = text;
                    File.WriteAllLines(path, allLines);
                }
                catch (IndexOutOfRangeException ex) { }
            } else
            {
                File.WriteAllText(path, text);
            } 
        }

        private void EntryBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Space)
            {

                //current bug: color system will recolor all instances of a word even if string in string
                //to combat this, loop to continuously recolor the rest of the text not attempted by user
                //should find a better way to fix this, not priority right now
                for (int i = currentWordIndex + 1; i < checkInputArr.Length; i++)
                {
                    FromTextPointer(mainText.Document.ContentStart, mainText.Document.ContentEnd, checkInputArr[i] + " ", chosenColor);
                }

                FromTextPointer(mainText.Document.ContentStart, mainText.Document.ContentEnd, checkInputArr[currentWordIndex + 1] + " ", nextColor);

                if (!string.IsNullOrWhiteSpace(entryBox.Text))
                {

                    String userInputWord = entryBox.Text.Trim();
                    entryBox.Text = String.Empty;

                    //section for calculating time elapsed and wpm 
                    if (currentWordIndex == 0)
                    {
                        sw = Stopwatch.StartNew();
                        Debug.WriteLine("Timer started");
                    }

                    if (currentWordIndex == 49)
                    {
                        sw.Stop();
                        //prevent user entry when words have ran out
                        entryBox.IsEnabled = false;
                        Debug.WriteLine("Timer stopped");
                    }

                    //check if user input matches current word in array
                    if (userInputWord == checkInputArr[currentWordIndex])
                    {
                        correctWords += 1;
                        FromTextPointer(mainText.Document.ContentStart, mainText.Document.ContentEnd, checkInputArr[currentWordIndex] + " ", correctColor);
                    } else
                    {
                        FromTextPointer(mainText.Document.ContentStart, mainText.Document.ContentEnd, checkInputArr[currentWordIndex] + " ", incorrectColor);
                    }

                    //calculate words per minute
                    try
                    {
                        long seconds = sw.ElapsedMilliseconds / 1000;
                        wpm = ((double)correctWords / seconds * 60);
                    }
                    catch (DivideByZeroException ex) 
                    {
                        Console.WriteLine("{0} Exception caught.", ex);
                    }

                    //calculate accuracy  
                    try
                    {
                        accuracy = ((double)correctWords / ((double)currentWordIndex + 1)) * (double)100;
                    } catch (DivideByZeroException ex) 
                    {
                        Console.WriteLine("{0} Exception caught.", ex);
                    }

                    //increment index to next word in array
                    currentWordIndex += 1;

                    //set label to accuracy and wpm
                    //make labels a bit nicer, instead of displaying NaN, displays "N/A"
                    if (Double.IsNaN(wpm)) {wpmNum.Content = "N/A";} 
                    else {wpmNum.Content = Math.Round(wpm, 0);}

                    if (Double.IsNaN(accuracy)) {accNum.Content = "N/A";} 
                    else {accNum.Content = Math.Round(accuracy, 0) + "%";} 
                } 
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Refresh();
            }
        }

        private void ThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
            accLabel.Visibility = Visibility.Hidden;
            accNum.Visibility = Visibility.Hidden;
            wpmLabel.Visibility = Visibility.Hidden;
            wpmNum.Visibility = Visibility.Hidden;
            RfrshBtn.Visibility = Visibility.Hidden;
            entryBox.Visibility = Visibility.Hidden;
            entryRect.Visibility = Visibility.Hidden;
            mainText.Visibility = Visibility.Hidden;
            saveThemeBtn.Visibility = Visibility.Visible;

            OliviaThemeBtn.Visibility = Visibility.Visible;
            DraculaThemeBtn.Visibility = Visibility.Visible;
            ModernDolchThemeBtn.Visibility = Visibility.Visible;
            AshesThemeBtn.Visibility = Visibility.Visible;
        }

        private void saveThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            accLabel.Visibility = Visibility.Visible;
            accNum.Visibility = Visibility.Visible;
            wpmLabel.Visibility = Visibility.Visible;
            wpmNum.Visibility = Visibility.Visible;
            RfrshBtn.Visibility = Visibility.Visible;
            entryBox.Visibility = Visibility.Visible;
            entryRect.Visibility = Visibility.Visible;
            mainText.Visibility = Visibility.Visible;
            saveThemeBtn.Visibility = Visibility.Hidden;

            OliviaThemeBtn.Visibility = Visibility.Hidden;
            DraculaThemeBtn.Visibility = Visibility.Hidden;
            ModernDolchThemeBtn.Visibility = Visibility.Hidden;
            AshesThemeBtn.Visibility = Visibility.Hidden;
        }

        private void OliviaThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            Olivia();
            File.WriteAllText("Olivia", "Theme.txt");
        }

        private void DraculaThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            Dracula();
        }

        private void ModernDolchThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            Dolche();
        }

        private void AshesThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            Ashes();
        }

        private void Olivia()
        {
            BrushConverter bc = new BrushConverter();
            MainGrid.Background = (Brush)bc.ConvertFrom("#302F2D");
            mainRect.Background = (Brush)bc.ConvertFrom("#D5D5CD");
            RfrshBtn.Background = (Brush)bc.ConvertFrom("#302F2D");
            RfrshBtn.Foreground = (Brush)bc.ConvertFrom("#DAAB99");
            ThemeBtn.Background = (Brush)bc.ConvertFrom("#3b3937");
            ThemeBtn.Foreground = (Brush)bc.ConvertFrom("#DAAB99");
            accNum.Foreground = (Brush)bc.ConvertFrom("#DAAB99");
            accLabel.Foreground = (Brush)bc.ConvertFrom("#DAAB99");
            wpmNum.Foreground = (Brush)bc.ConvertFrom("#DAAB99");
            wpmLabel.Foreground = (Brush)bc.ConvertFrom("#DAAB99");
            mainText.Foreground = (Brush)bc.ConvertFrom("#302F2D");
            entryBox.Foreground = (Brush)bc.ConvertFrom("#DAAB99");
            entryRect.Fill = (Brush)bc.ConvertFrom("#302F2D");
            correctColor = "#00b32a";
            chosenColor = "#302F2D";
            appendTheme(themePath, "Olivia");
        }

        private void Dracula()
        {
            BrushConverter bc = new BrushConverter();
            MainGrid.Background = (Brush)bc.ConvertFrom("#3B3F50");
            mainRect.Background = (Brush)bc.ConvertFrom("#23222E");
            RfrshBtn.Background = (Brush)bc.ConvertFrom("#383C4D");
            RfrshBtn.Foreground = (Brush)bc.ConvertFrom("#F7FAFD");
            ThemeBtn.Background = (Brush)bc.ConvertFrom("#23222E");
            ThemeBtn.Foreground = (Brush)bc.ConvertFrom("#F7FAFD");
            accNum.Foreground = (Brush)bc.ConvertFrom("#A790D6");
            accLabel.Foreground = (Brush)bc.ConvertFrom("#D88EC7");
            wpmNum.Foreground = (Brush)bc.ConvertFrom("#69E199");
            wpmLabel.Foreground = (Brush)bc.ConvertFrom("#F2FEA3");
            mainText.Foreground = (Brush)bc.ConvertFrom("#F2F2F5");
            entryBox.Foreground = (Brush)bc.ConvertFrom("#B9A0E8");
            entryRect.Fill = (Brush)bc.ConvertFrom("#393D4E");
            correctColor = "#00b32a";
            chosenColor = "#F2F2F5";
            appendTheme(themePath, "Dracula");
        }

        private void Dolche()
        {
            BrushConverter bc = new BrushConverter();
            MainGrid.Background = (Brush)bc.ConvertFrom("#7e8488");
            mainRect.Background = (Brush)bc.ConvertFrom("#5a5d64");
            RfrshBtn.Background = (Brush)bc.ConvertFrom("#69d6d1");
            RfrshBtn.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            ThemeBtn.Background = (Brush)bc.ConvertFrom("#69d6d1");
            ThemeBtn.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            accNum.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            accLabel.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            wpmNum.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            wpmLabel.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            mainText.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            entryBox.Foreground = (Brush)bc.ConvertFrom("#000000");
            entryRect.Fill = (Brush)bc.ConvertFrom("#ffffff");
            correctColor = "#69d6d1";
            incorrectColor = "#ff3b3b";
            chosenColor = "#ffffff";
            appendTheme(themePath, "Dolche");
        }

        private void Ashes()
        {
            BrushConverter bc = new BrushConverter();
            MainGrid.Background = (Brush)bc.ConvertFrom("#d1dadf");
            mainRect.Background = (Brush)bc.ConvertFrom("#62686e");
            RfrshBtn.Background = (Brush)bc.ConvertFrom("#3a3b3d");
            RfrshBtn.Foreground = (Brush)bc.ConvertFrom("#d3dae0");
            ThemeBtn.Background = (Brush)bc.ConvertFrom("#3a3b3d");
            ThemeBtn.Foreground = (Brush)bc.ConvertFrom("#ffffff");
            accNum.Foreground = (Brush)bc.ConvertFrom("#64696F");
            accLabel.Foreground = (Brush)bc.ConvertFrom("#64696F");
            wpmNum.Foreground = (Brush)bc.ConvertFrom("#64696F");
            wpmLabel.Foreground = (Brush)bc.ConvertFrom("#64696F");
            mainText.Foreground = (Brush)bc.ConvertFrom("#d3dae0");
            entryBox.Foreground = (Brush)bc.ConvertFrom("#d3dae0");
            entryRect.Fill = (Brush)bc.ConvertFrom("#3a3b3d");
            correctColor = "#00b32a";
            chosenColor = "#d3dae0";
            appendTheme(themePath, "Ashes");
        }

        //declaring variables
        public String[] textFieldArr;
        public String[] checkInputArr;
        public String[] recolorText = new string[50];
        public String tempText = "";
        public List list;
        public int currentWordIndex = 0;
        public int correctWords = 0;
        public double accuracy = 0;
        public Stopwatch sw = new Stopwatch();
        public double wpm = 0;
        public static BrushConverter bc = new BrushConverter();
        public String chosenColor = "#000000";
        public String correctColor = "#00b32a";
        public String incorrectColor = "#ff0000";
        public String nextColor = "#c848fa";
        public String themePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Theme.txt";
    }
}
