﻿/*
AUTOEQ_SELECTOR

MainWindow.cs

- Description: Main application window
- Author: David Molina Toro
- Date: 09 - 10 - 2019
- Version: 1.0

Property of application owner
*/

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Generic;

namespace AutoEQ_Selector
{
    public partial class MainWindow : Window
    {
        /*
        Public constructor
        */
        public MainWindow()
        {
            InitializeComponent();
        }

        //Common loop counter
        private int iCount = 0;

        //Common line storage
        private string sLine = "";

        //AutoEQ folder and configuration path
        private string sPathAutoEQ = "";
        private string sConfigFile = "";

        //Raw results directories
        private List<string> listResultsFiles;

        //Headphone models list
        private List<string> listModels = new List<string>();

        /*
        Main window loaded event
        */
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Get the application configuration
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = AppDomain.CurrentDomain.FriendlyName + ".config"
            };
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

            //Get the AutoEQ stored path
            sPathAutoEQ = config.AppSettings.Settings["AutoEQ_Folder"].Value;

            //Check the AutoEQ folder path
            if (string.IsNullOrEmpty(sPathAutoEQ) || !Directory.Exists(sPathAutoEQ))
            {
                //Hide the main application
                Visibility = Visibility.Hidden;

                //Select the AutoEQ folder
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.Description = "Select the AutoEQ folder";
                dialog.ShowDialog();

                //Save the AutoEQ folder
                sPathAutoEQ = dialog.SelectedPath;
                config.AppSettings.Settings["AutoEQ_Folder"].Value = dialog.SelectedPath;
                config.Save();
            }

            //Get the EqualizerAPO system path
            string sPathEqualizerAPO = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EqualizerAPO");

            //Check the EqualizerAPO folder path
            if (string.IsNullOrEmpty(sPathEqualizerAPO) || !Directory.Exists(sPathEqualizerAPO))
            {
                sPathEqualizerAPO = config.AppSettings.Settings["EqualizerAPO_Folder"].Value;
                if (string.IsNullOrEmpty(sPathEqualizerAPO) || !Directory.Exists(sPathEqualizerAPO))
                {
                    //Hide the main application
                    Visibility = Visibility.Hidden;

                    //Select the EqualizerAPO folder
                    FolderBrowserDialog dialog = new FolderBrowserDialog();
                    dialog.Description = "Select the EqualizerAPO folder";
                    dialog.ShowDialog();

                    //Save the EqualizerAPO folder
                    Properties.Settings.Default["EqualizerAPO_Folder"] = dialog.SelectedPath;
                    config.Save();
                }
            }

            //Set the configuration file path
            sConfigFile = Path.Combine(sPathEqualizerAPO, "config", "config.txt");

            //Get the headphone models
            iCount = 0;
            listResultsFiles = new List<string>(Directory.GetFiles(Path.Combine(sPathAutoEQ, "results"), "*GraphicEQ.txt", SearchOption.AllDirectories));
            while (iCount < listResultsFiles.Count)
            {
                //Get the current headphone model
                string[] sParts = listResultsFiles[iCount].Split('\\');

                //Check if the model is already in the list
                if (!listModels.Contains(sParts[sParts.Length - 2]))
                {
                    //Remove the results with explanations
                    if (!sParts[sParts.Length - 2].Contains("sample") && !sParts[sParts.Length - 2].Contains("(") && !sParts[sParts.Length - 2].Contains(")"))
                    {
                        listModels.Add(sParts[sParts.Length - 2]);
                    }
                    else
                    {
                        listResultsFiles.RemoveAt(iCount);
                        continue;
                    }
                }

                iCount++;
            }

            //Sort the model list alphabetically
            listModels.Sort();

            //Add the models to the list box
            for (int i = 0; i < listModels.Count; i++)
            {
                listBoxModels.Items.Add(listModels[i]);
            }

            //Show the main application
            Visibility = Visibility.Visible;
        }

        /*
        Close function
        */
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /*
        Mouse press function for press and move the whole window
        */
        private void GridTop_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        /*
        Models text box changed function
        */
        private void TextBoxModels_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //Reset the models list box
            listBoxModels.Items.Clear();

            //Check if there is a text for the search
            if (!string.IsNullOrEmpty(textBoxModels.Text))
            {
                //Find the models containing the search and add them
                for (int i = 0; i < listModels.Count; i++)
                {
                    if (listModels[i].ToUpper().Contains(textBoxModels.Text.ToUpper()))
                    {
                        listBoxModels.Items.Add(listModels[i]);
                    }
                }
            }
            else
            {
                //Add the models to the list box
                for (int i = 0; i < listModels.Count; i++)
                {
                    listBoxModels.Items.Add(listModels[i]);
                }
            }
        }

        /*
        Apply the selected configuration
        */
        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            //Check if there is a saleected model and test
            if (listBoxModels.SelectedIndex != -1 && listBoxTests.SelectedIndex != -1)
            {
                //Get the selected model and test
                for (int i = 0; i < listResultsFiles.Count; i++)
                {
                    string[] sParts = listResultsFiles[i].Split('\\');
                    if (sParts[sParts.Length - 4] + "\\" + sParts[sParts.Length - 3] == listBoxTests.SelectedItem.ToString() && sParts[sParts.Length - 2] == listBoxModels.SelectedItem.ToString())
                    {
                        //Get the graphic EQ values
                        string sGraphicEQ = File.ReadAllText(listResultsFiles[i]);

                        //Check if the selected file exists
                        if (File.Exists(sConfigFile))
                        {
                            //Check if the target file has some graphic EQ values already
                            StreamReader streamReader = new StreamReader(sConfigFile);

                            //Reset and create counters
                            iCount = 0;
                            bool bReplaced = false;
                            while ((sLine = streamReader.ReadLine()) != null)
                            {
                                if (sLine.Contains("GraphicEQ: "))
                                {
                                    //Close the stream reader
                                    streamReader.Close();

                                    //Store all the target file lines
                                    string[] sLines = File.ReadAllLines(sConfigFile);

                                    //Write the graphic EQ line
                                    StreamWriter streamWriter = new StreamWriter(sConfigFile, false);
                                    for (int k = 0; k < sLines.Length; ++k)
                                    {
                                        if (k == iCount)
                                        {
                                            streamWriter.WriteLine(sGraphicEQ);
                                        }
                                        else
                                        {
                                            streamWriter.WriteLine(sLines[k]);
                                        }
                                    }

                                    //Close the stream writer
                                    streamWriter.Close();

                                    bReplaced = true;
                                    break;
                                }

                                iCount++;
                            }

                            //Append the graphic EQ line if not replaced
                            if (!bReplaced)
                            {
                                //Close the stream reader
                                streamReader.Close();

                                //Write the graphic EQ line
                                StreamWriter streamWriter = new StreamWriter(sConfigFile, true);
                                streamWriter.Write(sGraphicEQ);

                                //Close the stream writer
                                streamWriter.Close();
                                break;
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("Check if the AutoEQ configuration file exists", "AutoEQ Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                            Console.WriteLine("Config: {0}", sConfigFile);
                        }
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Check if everything needed is selected", "AutoEQ Selector", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /*
        Reset all the interface
        */
        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            //Check if the selected file exists
            if (File.Exists(sConfigFile))
            {
                //Check if the target file has some graphic EQ values already
                StreamReader streamReader = new StreamReader(sConfigFile);

                //Close the stream reader
                streamReader.Close();

                //Store all the target file lines
                string[] sLines = File.ReadAllLines(sConfigFile);

                //Write the graphic EQ line
                StreamWriter streamWriter = new StreamWriter(sConfigFile, false);
                for (int i = 0; i < sLines.Length; i++)
                {
                    if (sLines[i].Contains("GraphicEQ: "))
                    {
                        streamWriter.WriteLine(sLines[i].Replace("GraphicEQ: ", "# GraphicEQ: "));
                    }
                    else
                    {
                        streamWriter.WriteLine(sLines[i]);
                    }
                }

                //Close the stream writer
                streamWriter.Close();
            }
            else
            {
                System.Windows.MessageBox.Show("Check if the AutoEQ configuration file exists", "AutoEQ Selector", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("Config: {0}", sConfigFile);
            }

            textBoxModels.Text = "";

            listBoxModels.Items.Clear();
            for (int i = 0; i < listModels.Count; i++)
            {
                listBoxModels.Items.Add(listModels[i]);
            }

            listBoxTests.Items.Clear();
        }

        /*
        Model changed function
        */
        private void ListBoxModels_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //Clear the test list box
            listBoxTests.Items.Clear();

            //Check if there is a selected model
            if (listBoxModels.SelectedIndex >= 0)
            {
                //Get the selected model
                string sModel = listBoxModels.SelectedItem.ToString();

                //Get the model test results
                for (int i = 0; i < listResultsFiles.Count; i++)
                {
                    string[] sParts = listResultsFiles[i].Split('\\');
                    if (sParts[sParts.Length - 2].Contains(sModel))
                    {
                        listBoxTests.Items.Add(sParts[sParts.Length - 4] + "\\" + sParts[sParts.Length - 3]);
                    }
                }
            }
        }
    }
}
