using NStack;
using System.Collections;
using Terminal.Gui;
using Application = Terminal.Gui.Application;
using Attribute = Terminal.Gui.Attribute;
using Label = Terminal.Gui.Label;

Application.Run<ExampleWindow>();

//Console.WriteLine($"Username: {((ExampleWindow)Application.Top).usernameText.Text}");

Application.Shutdown();

public class ExampleWindow : Window
{
    public CheckBox _customRenderCB;
    public CheckBox _allowMarkingCB;
    public CheckBox _allowMultipleCB;
    public ListView _listView;

    public ExampleWindow()
    {
        Title = "Hls Converter (Ctrl+Q to quit)";

        //_customRenderCB = new CheckBox("Render with columns")
        //{
        //    X = 0,
        //    Y = 0,
        //    Height = 1,
        //};
        //Add(_customRenderCB);
        //_customRenderCB.Toggled += _customRenderCB_Toggled;

        //_allowMarkingCB = new CheckBox("Allow Marking")
        //{
        //    X = Pos.Right(_customRenderCB) + 1,
        //    Y = 0,
        //    Height = 1,
        //};
        //Add(_allowMarkingCB);
        //_allowMarkingCB.Toggled += AllowMarkingCB_Toggled;

        //_allowMultipleCB = new CheckBox("Allow Multi-Select")
        //{
        //    X = Pos.Right(_allowMarkingCB) + 1,
        //    Y = 0,
        //    Height = 1,
        //    Visible = _allowMarkingCB.Checked
        //};
        //Add(_allowMultipleCB);
        //_allowMultipleCB.Toggled += AllowMultipleCB_Toggled;

        _listView = new ListView()
        {
            X = 1,
            Y = 2,
            Height = Dim.Fill(),
            Width = Dim.Fill(1),
            //ColorScheme = Colors.TopLevel,
            AllowsMarking = true,
            AllowsMultipleSelection = true
        };
        _listView.RowRender += ListView_RowRender;
        Add(_listView);

        var _scrollBar = new ScrollBarView(_listView, true);

        _scrollBar.ChangedPosition += () =>
        {
            _listView.TopItem = _scrollBar.Position;
            if (_listView.TopItem != _scrollBar.Position)
            {
                _scrollBar.Position = _listView.TopItem;
            }
            _listView.SetNeedsDisplay();
        };

        _scrollBar.OtherScrollBarView.ChangedPosition += () =>
        {
            _listView.LeftItem = _scrollBar.OtherScrollBarView.Position;
            if (_listView.LeftItem != _scrollBar.OtherScrollBarView.Position)
            {
                _scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
            }
            _listView.SetNeedsDisplay();
        };

        _listView.DrawContent += (e) =>
        {
            _scrollBar.Size = _listView.Source.Count - 1;
            _scrollBar.Position = _listView.TopItem;
            _scrollBar.OtherScrollBarView.Size = _listView.Maxlength - 1;
            _scrollBar.OtherScrollBarView.Position = _listView.LeftItem;
            _scrollBar.Refresh();
        };

        List<(bool IsMarked, string Path)> List = new()
        {
            new (true, "1"),
            new (false, "2"),
            new (true, "3"),
            new (true, "4"),
            new (false, "5"),
        };

        _listView.SetSource(List);

        var k = "Keep Content Always In Viewport";
        var keepCheckBox = new CheckBox(k, _scrollBar.AutoHideScrollBars)
        {
            X = Pos.AnchorEnd(k.Length + 3),
            Y = 0,
        };
        keepCheckBox.Toggled += (_) => _scrollBar.KeepContentAlwaysInViewport = keepCheckBox.Checked;
        Add(keepCheckBox);
    }

    private void ListView_RowRender(ListViewRowEventArgs obj)
    {
        if (obj.Row == _listView.SelectedItem)
        {
            return;
        }
        if (_listView.AllowsMarking && _listView.Source.IsMarked(obj.Row))
        {
            obj.RowAttribute = new Attribute(Color.BrightRed, Color.BrightYellow);
            return;
        }
        if (obj.Row % 2 == 0)
        {
            obj.RowAttribute = new Attribute(Color.BrightGreen, Color.Magenta);
        }
        else
        {
            obj.RowAttribute = new Attribute(Color.BrightMagenta, Color.Green);
        }
    }

}

public class ListHlsDataSource : IListDataSource
{
    private List<(bool IsMarked, string Path)> List = new()
    {
        new (true, "1"),
        new (false, "2"),
        new (true, "3"),
        new (true, "4"),
        new (false, "5"),
    };

    int _nameColumnWidth = 30;
    public ListHlsDataSource(List<(bool IsMarked, string Path)> itemList) => List = itemList;

    public int Count => List.Count;

    public int Length => List.Count;

    public bool IsMarked(int item)
    {
        if (item >= 0 && item < Count)
            return List[item].IsMarked;
        return false;
    }

    public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
    {
        container.Move(col, line);
        // Equivalent to an interpolated string like $"{Scenarios[item].Name, -widtestname}"; if such a thing were possible
        var s = String.Format(String.Format("{{0,{0}}}", -_nameColumnWidth), List[item].Path);
        RenderUstr(driver, $"{s}  ", col, line, width, start);
    }

    public void SetMark(int item, bool value)
    {
        if (item >= 0 && item < Count)
        {
            var getItem = List[item];
            getItem.IsMarked = value;
        }
    }

    public IList ToList()
    {
        return List;
    }

    private void RenderUstr(ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
    {
        int used = 0;
        int index = start;
        while (index < ustr.Length)
        {
            (var rune, var size) = Utf8.DecodeRune(ustr, index, index - ustr.Length);
            var count = Rune.ColumnWidth(rune);
            if (used + count >= width) break;
            driver.AddRune(rune);
            used += count;
            index += size;
        }

        while (used < width)
        {
            driver.AddRune(' ');
            used++;
        }
    }

}