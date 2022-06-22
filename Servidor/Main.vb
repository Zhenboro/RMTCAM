Imports System.Net.Sockets
Imports System.Net
Imports System.Runtime.Serialization.Formatters.Binary
Public Class Main
    Dim YO As TcpListener
    Dim REMOTO As TcpClient
    Dim threadRecibe As Threading.Thread
    Dim threadEnvia As Threading.Thread
    Dim NS As NetworkStream
    Dim envioMensaje As String

    Dim RESOLUCIONX As Integer
    Dim RESOLUCIONY As Integer
    Dim POSICIONX As Integer
    Dim POSICIONY As Integer

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StartUp.Init()
        CheckForIllegalCrossThreadCalls = False
    End Sub

    Sub Iniciar()
        Try
            Button1.Enabled = False
            Button2.Enabled = True
            Label1.Enabled = True
            ComboBox1.Enabled = True
            Button3.Enabled = True
            SaveMemory()
            YO = New TcpListener(IPAddress.Any, ServerPort)
            YO.Start()
            threadRecibe = New Threading.Thread(AddressOf RECIBIR)
            threadRecibe.Start()
            threadEnvia = New Threading.Thread(AddressOf ENVIAR)
            threadEnvia.Start()
        Catch ex As Exception
            AddToLog("Iniciar@Main", "Error: " & ex.Message, True)
        End Try
    End Sub
    Sub Detener()
        Try
            Button1.Enabled = True
            Button2.Enabled = False
            Label1.Enabled = False
            ComboBox1.Enabled = False
            Button3.Enabled = False
            YO.Stop()
            threadRecibe.Abort()
            threadEnvia.Abort()
            REMOTO.Close()
        Catch ex As Exception
            AddToLog("Detener@Main", "Error: " & ex.Message, True)
            End
        End Try
        End
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Iniciar()
    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Detener()
    End Sub

    Sub RECIBIR()
        Dim BF As New BinaryFormatter
        While True
            Try
                REMOTO = YO.AcceptTcpClient()
                NS = REMOTO.GetStream
                While REMOTO.Connected = True
                    Dim content = BF.Deserialize(NS)
                    Dim analizador As String = System.Text.Encoding.UTF7.GetString(content)
                    If analizador.StartsWith("[") Then
                        ProcesarMensaje(content)
                    Else
                        PictureBox1.Image = content
                        RESOLUCIONX = PictureBox1.Image.Width
                        RESOLUCIONY = PictureBox1.Image.Height
                    End If
                End While
            Catch ex As Exception
                AddToLog("RECIBIR@Main", "Error: " & ex.Message, True)
            End Try
        End While
    End Sub
    Sub ProcesarMensaje(ByVal mensaje As String)
        Try
            If mensaje.StartsWith("[CameraList]") Then
                Dim camaras() = mensaje.Replace("[CameraList]", Nothing).Split("|")
                For Each camara As String In camaras
                    ComboBox1.Items.Add(camara)
                Next
            ElseIf mensaje.StartsWith("[---]") Then

            End If
        Catch ex As Exception
            AddToLog("ProcesarMensaje@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Sub ENVIAR()
        Dim BF As New BinaryFormatter
        While True
            Try
                If NS IsNot Nothing Then
                    NS = REMOTO.GetStream
                    If envioMensaje IsNot Nothing Then
                        BF.Serialize(NS, System.Text.Encoding.UTF7.GetBytes(envioMensaje))
                        envioMensaje = Nothing
                    End If
                End If
                Threading.Thread.Sleep(3000) '3 sec
            Catch ex As Exception
                AddToLog("ENVIAR@Main", "Error: " & ex.Message, True)
            End Try
        End While
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        envioMensaje = "[CameraSelect]|" & ComboBox1.SelectedIndex
        Button3.Enabled = False
    End Sub
End Class
'el mensaje CameraList no se procesa bien.