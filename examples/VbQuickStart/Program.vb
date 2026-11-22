' The SDK from Visual Basic. Set AGILIS_API_KEY to a pk_test_ key, then:
'   dotnet run --project examples/VbQuickStart
Imports System
Imports System.Threading.Tasks
Imports Agilis.Ads

Module Program

    Function Main() As Integer
        Return RunAsync().GetAwaiter().GetResult()
    End Function

    Private Async Function RunAsync() As Task(Of Integer)
        Using client As New AgilisClient()
            If client.Mode <> AgilisMode.Test Then
                Console.Error.WriteLine("This sample runs with a Test-mode (pk_test_) key only.")
                Return 1
            End If

            Dim categories As CategoryList = Await client.Categories.ListAsync()
            For Each category As Category In categories.Categories
                Console.WriteLine($"{category.CategoryId}: {category.CategoryName}")
            Next

            Dim page As Page(Of AgilisEvent) = Await client.Events.ListAsync(New EventListParams With {.Limit = 10})
            For Each listed As AgilisEvent In page.Data
                Console.WriteLine($"Event {listed.EventId}: {listed.Title} ({listed.EventStatus})")
            Next

            Try
                Dim config As EventConfig = Await client.EventConfig.GetAsync()
                Console.WriteLine($"Publishing an Event costs {config.ListingFee} {config.Currency}.")
            Catch ex As AgilisException
                Console.WriteLine($"Refused ({ex.StatusCode}, {ex.ReturnCode}): {ex.Message}")
            End Try
        End Using

        Return 0
    End Function

End Module
