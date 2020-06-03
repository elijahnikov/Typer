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
                    int r = rnd.Next(textFieldArr.Length);
                    tempText += textFieldArr[r] + " ";
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
                        System.Diagnostics.Debug.WriteLine("Timer stopped");
                    }

                    //check if user input matches current word in array
                    if (userInputWord == checkInputArr[currentWordIndex])
                    {
                        correctWords += 1;
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

                    //set label to accuracy and wpm num
                    wpmNum.Content = Math.Round(wpm, 0);
                    accNum.Content = Math.Round(accuracy, 0) + "%";
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
    }
}
