﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {
        string name;
        bool terminating = false;
        bool connected = false;
        Socket clientSocket;


        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            terminating = false; // to connect after disconnect
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_IP.Text;
            int portNum;
            name = textBox_Name.Text;
            string serverRespond = "";

            if (name != "" && name.Length <= 64) // if name is not empty and longer than 64
            {
                if (Int32.TryParse(textBox_Port.Text, out portNum))
                {
                    try
                    {
                        clientSocket.Connect(IP, portNum);
                        send_message(name); // we send our username to server and wait for respond
                        serverRespond = receiveOneMessage(); // we got our respond
                        if (serverRespond != "already connected" && serverRespond != "not authorized")
                        {
                            button_accept.Enabled = true;
                            button_reject.Enabled = true;
                            button_invite.Enabled = true;
                            button_connect.Enabled = false;
                            button_disconnect.Enabled = true;
                            button_sendmessage.Enabled = true;
                            textBox_Message.Enabled = true;
                            connected = true;
                            button_connect.Text = "Connected";
                            button_connect.BackColor = System.Drawing.Color.Green;
                            button_disconnect.BackColor = System.Drawing.Color.Red;
                            logBox.AppendText("Connection established...\n");
                            logBox.ScrollToCaret();

                            Thread receiveThread = new Thread(Receive);
                            receiveThread.Start();
                        }
                        else if (serverRespond == "not authorized")
                        {
                            logBox.AppendText("You are not registered.\n");
                            logBox.ScrollToCaret();
                        }
                        else if (serverRespond == "already connected")
                        {
                            logBox.AppendText("You are already connected.\n");
                            logBox.ScrollToCaret();
                        }
                    }
                    catch
                    {
                        logBox.AppendText("Could not connect to the server!\n");
                        logBox.ScrollToCaret();
                    }
                }
                else
                {
                    logBox.AppendText("Check the port\n");
                    logBox.ScrollToCaret();
                }
            }
            else
            {
                logBox.AppendText("Check the name\n");
                logBox.ScrollToCaret();
            }
        }

        private string receiveOneMessage() // this function receives only one message
        {
            Byte[] buffer = new Byte[64];
            clientSocket.Receive(buffer);
            string incomingMessage = Encoding.Default.GetString(buffer);
            incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
            return incomingMessage;
        }

        private void Receive()
        {
            while (connected)
            {
                try
                {
                    string incomingMessage = receiveOneMessage();
                    if (incomingMessage.Contains("R-Q-S-T-D-SEC-KEY"))
                    {
                        string newMes = incomingMessage.Substring(17);
                        friendRequestsCheckedList.Items.Add(newMes);
                    }
                    else if (incomingMessage.Contains("A-C-P-T-D-SEC-KEY"))
                    {
                        string newMes = incomingMessage.Substring(17) + "\n";
                        friendListBox.AppendText(newMes);
                    }
                    else if (incomingMessage.Contains("R-J-C-T-D-SEC-KEY"))
                    {
                        string newMes = incomingMessage.Substring(17) + "\n";
                        friendListBox.AppendText(newMes);
                    }
                    else
                    {
                        logBox.AppendText(incomingMessage);
                        logBox.ScrollToCaret();
                    }
                }
                catch
                {
                    if (!terminating)
                    {
                        button_connect.Enabled = true;
                        button_disconnect.Enabled = false;
                        button_sendmessage.Enabled = false;
                        textBox_Message.Enabled = false;
                        clientSocket.Disconnect(true);
                        logBox.AppendText("The server has disconnected\n");
                        logBox.ScrollToCaret();
                    }
                    clientSocket.Close();
                    connected = false;
                }
            }
        }

        private void send_message(string message)
        {
            Byte[] buffer = new Byte[64];
            buffer = Encoding.Default.GetBytes(message);
            clientSocket.Send(buffer);
        }

        private void button_sendmessage_Click(object sender, EventArgs e)
        {
            string message = textBox_Message.Text;
            if (message != "" && message.Length <= 64)
            {
                textBox_Message.Text = "";
                send_message(message);
                logBox.AppendText( "Me: " + message + "\n");
                logBox.ScrollToCaret();
            }

        }

        private void button_disconnect_Click(object sender, EventArgs e)
        {
            button_accept.Enabled = false;
            button_reject.Enabled = false;
            button_invite.Enabled = false;
            button_disconnect.BackColor = default(Color);
            button_connect.BackColor = default(Color);
            button_connect.Text = "Connect";
            send_message("D-I-S-C-O-N-N-E-C-T-E-D-SEC-KEY");
            connected = false;
            button_connect.Enabled = true;
            button_disconnect.Enabled = false;
            button_sendmessage.Enabled = false;
            textBox_Message.Enabled = false;
            clientSocket.Disconnect(false);
            logBox.AppendText("Disconnected\n");
            logBox.ScrollToCaret();
        }

        private void button_invite_Click(object sender, EventArgs e)
        {
            string message = textBox_invite.Text;
            if (message != "" && message.Length <= 64)
            {
                logBox.AppendText("Friend request sent to " + message + "\n");
                message = "I-N-V-SEC-KEY" + message;
                textBox_invite.Text = "";
                send_message(message);
            }
        }

        List<string> getCheckedItems()
        {
            List<string> friendRequests = new List<string>();
            for (int i = 0; i < friendRequestsCheckedList.Items.Count; i++)
            {
                if (friendRequestsCheckedList.GetItemChecked(i))
                {
                    friendRequests.Add(friendRequestsCheckedList.Items[i].ToString());
                    friendRequestsCheckedList.Items.Remove(friendRequestsCheckedList.Items[i]);
                }
            }
            return friendRequests;
        }

        private void button_accept_Click(object sender, EventArgs e)
        {
            List<string> selectedNames = getCheckedItems(); 
            foreach (string name in selectedNames)
            {
                logBox.AppendText("Accepted friend request of " + name + "\n");
                string message = "A-C-P-T-SEC-KEY" + name;
                textBox_invite.Text = "";
                send_message(message);
            }
        }

        private void button_reject_Click(object sender, EventArgs e)
        {
            List<string> selectedNames = getCheckedItems();
            foreach (string name in selectedNames)
            {
                logBox.AppendText("Rejected friend request of " + name + "\n");
                string message = "R-J-C-T-SEC-KEY" + name;
                textBox_invite.Text = "";
                send_message(message);
            }
        }
    }
}
