using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Security.Cryptography;

namespace DataHealthCheck
{
    public partial class Form1 : Form
    {
        int corrupted = 0;
        int missing = 0;
        int changed = 0;
        int unchanged = 0;
        int newFiles = 0;

        string corruptLogPath;
        string hashLogPath;
        string newHashLogPath;
 
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        int isfolder;

        private static string[] items;

        public Form1()
        {
            InitializeComponent();
        }
        List<string> filesList = new List<string>();
        private void customGetFiles(string folder)
        {
            foreach (string file in Directory.GetFiles(folder))
            {
                filesList.Add(file);
            }
            foreach (string subDir in Directory.GetDirectories(folder))
            {
                try
                {
                    customGetFiles(subDir);
                }
                catch
                {}
            }
        }
        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }       
        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            items = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string item in items)
            {
               listBox1.Items.Add(item);
            }

            backgroundWorker3.RunWorkerAsync();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            backgroundWorker2.RunWorkerAsync();
            sw.Start();
        }
        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {   
            //for each item in listbox
            foreach (string item in listBox1.Items)
            {
                if (Directory.Exists(item))
                {   
                    string folderName = Path.GetFileName(item);
                    hashLogPath = item + @"\" + folderName + ".hashes.txt";

                    isfolder = 1;

                    //building list of files in folder
                    filesList.Clear();
                    customGetFiles(item);

                    //if old hashlog doesn't exists we create it
                    if (!File.Exists(hashLogPath))
                    {
                        StreamWriter writer = new StreamWriter(hashLogPath);
                        writer.Close();
                    }

                    //open old hashlog in readmode
                    StreamReader reader = new StreamReader(hashLogPath);

                    //open new hashlog in writemode
                    newHashLogPath = item + @"\" + folderName + ".hashes.txt.temp";
                    StreamWriter newHashLogWriter = new StreamWriter(newHashLogPath);

                    //creating corrupt log
                    corruptLogPath = item + @"\" + folderName + ".corrupt.txt";
                    StreamWriter writercorrupt = new StreamWriter(corruptLogPath);

                    //do this for each file in folder
                    foreach (string fileString in filesList)
                    {
                        if (fileString != hashLogPath && fileString != newHashLogPath && fileString != corruptLogPath && IsFileNotLocked(fileString))
                        {
                            //writing computing md5 in interface
                            textBox1.Invoke(new MethodInvoker(delegate
                            {
                                textBox1.AppendText("Checking " + "\"" + fileString + "\"");
                                textBox1.AppendText(Environment.NewLine);
                            }));

                            //computing md5
                            md5Class.md5Method(fileString);
                            //end: computing md5

                            int realFileFound = 0;

                            //getting last modification date of current file
                            DateTime dt = File.GetLastWriteTime(fileString);

                            //reading each line of old hash log
                            string line;
                            string[] data;
                            while ((line = reader.ReadLine()) != null)
                            {
                                //extracting data from line
                                data = line.Split('|');
                                string relativePath = data[0];
                                string md5 = data[1];
                                string modDate = data[2];

                                //building full path of file
                                string fullPath = item + relativePath;

                                //found the file in the db corresponding to current file
                                if (fullPath == fileString)
                                {
                                    realFileFound = 1;

                                    //determining file state
                                    if (md5Class.md5String != md5)
                                    {
                                        //file has changed or is corrupt
                                        if (dt.ToString() == modDate)
                                        {
                                            //file is corrupt
                                            //textBox1.Invoke(new MethodInvoker(delegate
                                            //{
                                            //    textBox1.AppendText("\"" + fullPath + "\"" + ": corrupt.");
                                            //    textBox1.AppendText(Environment.NewLine);
                                            //}));

                                            writercorrupt.WriteLine(fullPath);
                                            corrupted++;

                                            newHashLogWriter.WriteLine(relativePath + "|" + md5 + "|" + dt);
                                        }
                                        else
                                        {
                                            //file is changed
                                            //textBox1.Invoke(new MethodInvoker(delegate
                                            //{
                                            //    textBox1.AppendText("\"" + fullPath + "\"" + ": changed. Updating hash in database.");
                                            //    textBox1.AppendText(Environment.NewLine);
                                            //}));
                                            changed++;

                                            newHashLogWriter.WriteLine(relativePath + "|" + md5Class.md5String + "|" + dt);
                                        }
                                    }
                                    else
                                    {
                                        //textBox1.Invoke(new MethodInvoker(delegate
                                        //{
                                        //    textBox1.AppendText("\"" + fullPath + "\"" + ": unchanged.");
                                        //    textBox1.AppendText(Environment.NewLine);
                                        //}));
                                        unchanged++;

                                        newHashLogWriter.WriteLine(relativePath + "|" + md5Class.md5String + "|" + dt);
                                    }
                                    //end of determining file state
                                }
                                //end: we found the file in the list corresponding to our real file
                            }
                            //end: reading each line of reader

                            //current file was not found in db
                            if (realFileFound != 1)
                            {
                                string relativePathOfNewFile = fileString.Replace(item, "");
                                newHashLogWriter.WriteLine(relativePathOfNewFile + "|" + md5Class.md5String + "|" + dt);

                                //textBox1.Invoke(new MethodInvoker(delegate
                                //{
                                //    textBox1.AppendText("\"" + fileString + "\"" + ": new file. Added hash to database.");
                                //    textBox1.AppendText(Environment.NewLine);
                                //}));
                                newFiles++;
                            }

                            //reseting old hash log reader position to 0
                            reader.DiscardBufferedData();
                            reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        }
                    }
                    //end: do this for each file in folder

                    //closing new hash log
                    newHashLogWriter.Close();

                    //closing corrupt log and delete it if empty
                    writercorrupt.Close();
                    if (new FileInfo(corruptLogPath).Length == 0)
                    {
                        // empty
                        File.Delete(corruptLogPath);
                    }

                    //closing old hashlog
                    reader.Close();

                    //replacing old hash log with new
                    System.IO.File.Delete(hashLogPath);
                    System.IO.File.Move(newHashLogPath, hashLogPath);
                }
                //else
                ////item is a file
                //{
                //    textBox1.Invoke(new MethodInvoker(delegate
                //    {
                //        // Execute the following code on the GUI thread.
                //        //listBox2.Items.Add("Done: " + hash);
                //        textBox1.AppendText("Item is not a folder. Skipping.");
                //        textBox1.AppendText(Environment.NewLine);
                //        //int visibleItems = listBox2.ClientSize.Height / listBox2.ItemHeight;
                //        //listBox2.TopIndex = Math.Max(listBox2.Items.Count - visibleItems + 1, 0);
                //    }));
                //}        
            }
            //end: for each item in listbox
        }
        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
           
            if (isfolder == 1)
            {
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = String.Format("{0:00} hours {1:00} minutes {2:00} seconds {3:00} milliseconds.",ts.Hours, ts.Minutes, ts.Seconds,ts.Milliseconds / 10);

                textBox1.AppendText("Hashes computed in " + elapsedTime);
                textBox1.AppendText(Environment.NewLine);

                textBox1.AppendText(unchanged + " file(s) unchanged.");
                textBox1.AppendText(Environment.NewLine);

                textBox1.AppendText(changed + " file(s) changed. (Last modification date: changed) (Content: changed) Hash(es) updated in database.");
                textBox1.AppendText(Environment.NewLine);
                
                textBox1.AppendText(newFiles + " new file(s) found. Hash(es) added to database.");
                textBox1.AppendText(Environment.NewLine);
                
                textBox1.AppendText(corrupted + " file(s) corrupted. (Last modification date: unchanged) (Content: changed) See log(s) in hashed folder(s).");
                textBox1.AppendText(Environment.NewLine);
            }

            sw.Stop();
            sw.Reset();

            Console.Beep();

            isfolder = 0;

            corrupted = 0;
            missing = 0;
            changed = 0;
            unchanged = 0;
            newFiles = 0;

            button2.Enabled = true;

            listBox1.Items.Clear();
        }
        public bool IsFileNotLocked(string filePath)
        {
            try
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }
        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (string item in items)
            {
                //if (Directory.Exists(item))
                //{
                //    textBox1.Invoke(new MethodInvoker(delegate
                //    {
                //        textBox1.AppendText("Folder detected.");
                //        textBox1.AppendText(Environment.NewLine);
                //    })); 
                //}
                if (File.Exists(item) && IsFileNotLocked(item))
                {
                    //item is a file

                    if (radioButtonMd5.Checked){
                        textBox1.Invoke(new MethodInvoker(delegate
                        {
                            textBox1.AppendText("Computing md5 hash of \"" + item + "\"");
                            textBox1.AppendText(Environment.NewLine);
                        }));

                        sw.Start();
                        md5Class.md5Method(item); //computing md5
                        TimeSpan ts = sw.Elapsed;
                        sw.Stop();
                        sw.Reset();
                        string elapsedTime = String.Format("{0:00} hours {1:00} minutes {2:00} seconds {3:00} milliseconds.", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    
                        textBox1.Invoke(new MethodInvoker(delegate
                        {
                            textBox1.AppendText("md5: " + md5Class.md5String + " computed in "+elapsedTime);
                            textBox1.AppendText(Environment.NewLine);
                        }));
                    }else{
                        textBox1.Invoke(new MethodInvoker(delegate
                        {
                            textBox1.AppendText("Computing sha1 hash of \"" + item + "\"");
                            textBox1.AppendText(Environment.NewLine);
                        }));

                        sw.Start();
                        sha1Class.sha1Method(item); //computing sha1
                        TimeSpan ts = sw.Elapsed;
                        sw.Stop();
                        sw.Reset();
                        string elapsedTime = String.Format("{0:00} hours {1:00} minutes {2:00} seconds {3:00} milliseconds.", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                        textBox1.Invoke(new MethodInvoker(delegate
                        {
                            textBox1.AppendText("sha1: " + sha1Class.sha1String + " computed in " + elapsedTime);
                            textBox1.AppendText(Environment.NewLine);
                        }));
                    }
                }
            }
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://cryptogeek.ninja/");
        }
    }
}
