Public Class Form4

    Private Sub Form4_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        Select Case grade
            Case 0
                Label2.Text = "حظ أوفر المرة القادمة"
            Case 1
                Label2.Text = "أحسنت"
            Case 2
                Label2.Text = "رائع"
        End Select

        Label1.Text = " حصلت يا " & Name & " على درجة " & grade

    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        End
    End Sub
End Class