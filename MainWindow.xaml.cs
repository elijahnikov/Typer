using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Documents;

namespace Typer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {  
            InitializeComponent();
            BrushConverter bc = new BrushConverter();
            MainGrid.Background = (Brush)bc.ConvertFrom("#FAFAFA");
            textFieldArr = Properties.Resources.TextFile.Split(" ");
            SetTextBox();
            entryBox.Focus();
        }
        private void Refresh()
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
        public static void FromTextPointer(TextPointer startPointer, TextPointer endPointer, string keyword, Brush foreground)
        {
            if (startPointer == null) throw new ArgumentNullException(nameof(startPointer));
            if (endPointer == null) throw new ArgumentNullException(nameof(endPointer));
            if (string.IsNullOrEmpty(keyword)) throw new ArgumentNullException(keyword);

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

                        selection.ApplyPropertyValue(TextElement.ForegroundProperty, foreground);
                    }
                }
                current = current.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        private void EntryBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Space)
            {

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
                        FromTextPointer(mainText.Document.ContentStart, mainText.Document.ContentEnd, checkInputArr[currentWordIndex] + " ", Brushes.Green);
                    } else
                    {
                        FromTextPointer(mainText.Document.ContentStart, mainText.Document.ContentEnd, checkInputArr[currentWordIndex] + " ", Brushes.Red);
                    }

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

                    //set colour of accuracy number
                    if (accuracy >= 75)
                    {
                        accNum.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 176, 15));
                    } else if (accuracy < 75 && accuracy > 40)
                    {
                        accNum.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(194, 187, 0));
                    } else
                    {
                        accNum.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(194, 0, 32));
                    }

                    //set label to accuracy and wpm
                    //make labels a bit nicer, instead of displaying NaN, displays "N/A"
                    if (Double.IsNaN(wpm))
                    {
                        wpmNum.Content = "N/A";
                    } else
                    {
                        wpmNum.Content = Math.Round(wpm, 0);
                    }

                    if (Double.IsNaN(accuracy))
                    {
                        accNum.Content = "N/A";
                    } else
                    {
                        accNum.Content = Math.Round(accuracy, 0) + "%";
                    }
                    
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

        //declaring variables
        public String[] textFieldArr;
        public String[] checkInputArr;
        public String tempText = "";
        public int currentWordIndex = 0;
        public int correctWords = 0;
        public double accuracy = 0;
        public Stopwatch sw = new Stopwatch();
        public double wpm = 0;

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
        }
    }
}
