Public Class Form1
    Const c_name = "علي أحمد"
    Const p_word = 11



    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        If TextBox1.Text = "" Then
            MsgBox("أدخل الأسم")
            Return
        End If

        If TextBox2.Text = p_word Then
            Name = TextBox1.Text
            Me.Hide()
            Form2.Show()
        Else
            MsgBox("ادخل كلمة السر بشكل صحيح")
        End If

    End Sub

    Private Sub Form1_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Label4.Text = c_name
        TextBox2.Text = " "
    End Sub

    Private Sub Button1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Button1.DoubleClick

    End Sub
End Class
