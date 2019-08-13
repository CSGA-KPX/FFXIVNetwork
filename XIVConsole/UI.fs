module UI
open System
open System.Windows.Forms


let GetListView () =
    let list = new ListView(
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines     = true,
                    AllowColumnReorder = true,
                    MultiSelect   = true,
                    Dock          = DockStyle.Fill)
        
    [| "Query"; "Item"; "Price"; "Count"; "Total"; "Last Update" |]
    |> Array.iter (fun x -> list.Columns.Add(x).Width <- -2)

    let keydown (e : KeyEventArgs) =
        if e.Control then
            match e.KeyCode with
            | Keys.A ->
                e.SuppressKeyPress <- true
                for item in list.Items do 
                    item.Selected <- true
            | Keys.C ->
                e.SuppressKeyPress <- true
                let sb = new Text.StringBuilder()
                let arr = 
                    [|
                        for header in list.Columns do 
                            yield header.Text
                    |]
                sb.AppendLine(String.Join("\t", arr)) |> ignore
                for item in list.SelectedItems do 
                    let arr = 
                        [|
                            for subitem in item.SubItems do 
                                yield subitem.Text
                        |]
                    sb.AppendLine(String.Join("\t", arr)) |> ignore
                Clipboard.SetText(sb.ToString())
            | _ ->
                e.SuppressKeyPress <- false
    list.KeyDown.Add(keydown)
    list

type MainForm () as this = 
    inherit Form()
    let textBox1 = new TextBox()
    let tabControl1 = new TabControl()
    do
        this.SuspendLayout();
        // 
        // textBox1
        // 
        textBox1.Dock <- System.Windows.Forms.DockStyle.Top;
        textBox1.Location <- new System.Drawing.Point(0, 0);
        textBox1.Name <- "textBox1";
        textBox1.Size <- new System.Drawing.Size(715, 21);
        textBox1.TabIndex <- 0;
        textBox1.KeyDown.AddHandler(new KeyEventHandler(this.TextBox1_KeyDown))
        // 
        // tabControl1
        // 
        tabControl1.Dock <- System.Windows.Forms.DockStyle.Fill;
        tabControl1.Location <- new System.Drawing.Point(0, 21);
        tabControl1.Name <- "tabControl1";
        tabControl1.SelectedIndex <- 0;
        tabControl1.Size <- new System.Drawing.Size(715, 368);
        tabControl1.TabIndex <- 1;
        tabControl1.KeyDown.Add(fun e ->
            if e.Control && (e.KeyCode = Keys.X) && not (isNull tabControl1.SelectedTab) then
                tabControl1.TabPages.Remove(tabControl1.SelectedTab)
        )
        // 
        // Form1
        // 
        this.AutoScaleDimensions <- new System.Drawing.SizeF(6.0F, 12.0F);
        this.AutoScaleMode <- System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize <- new System.Drawing.Size(715, 389);
        this.Controls.Add(tabControl1);
        this.Controls.Add(textBox1);
        this.Name <- "Form1";
        this.Text <- "XIVConsole";
        this.ResumeLayout(false);
        this.PerformLayout();

    member private x.TextBox1_KeyDown sender e =
        // 查询名: a,b,c,d,e 查物品
        // 查询名: !a,!b,!c 查配方
        // 查询名: !!a, !!b, !!c 查完整配方
        if e.KeyCode = Keys.Enter then
            let line = 
                textBox1.Text.Replace(" ", String.Empty).Replace("\t", String.Empty)
                    .Replace('！', '!')
            let (title, queries) = 
                let sp = line.Split(':', '：')
                if sp.Length <= 1 then
                    ("None", line.Split(',', '，') |> Array.map (Utils.StringQuery.FromString))
                else
                    (sp.[0], sp.[1].Split(',', '，') |> Array.map (Utils.StringQuery.FromString))
            x.TestAsync(title, queries)

    member private x.TestAsync (title, queries) = 
        let tp = new TabPage(title)
        
        Threading.Tasks.Task.Run(new Action(fun () -> 
            x.Test(title, queries, tp)
              )) |> ignore

        tabControl1.TabPages.Add(tp)
        tabControl1.SelectedTab <- tp

    member private x.Test (title, queries, tp) = 
        let list = GetListView()
        let buf = Collections.Generic.List<ListViewItem>()
        let addList query iname std count total update = 
            buf.Add(new ListViewItem([| query; iname; std; count; total; update |]))
        //先把文本查询转换好
        let queries = 
            queries
            |> Array.map (fun x -> x.GetOP())
            |> Array.map (fun arr -> 
                arr |> Array.Parallel.map (fun x -> x.Fetch()))

        let mutable sum = Utils.StdEv.Zero
        let cutOff = 25
        for q in queries do 
            for op in q do 
                match op with
                | Utils.DisplayOP.EmptyLine ->
                    addList "" "" "" "" "" ""
                | Utils.DisplayOP.BeginSum  -> sum <- Utils.StdEv.Zero
                | Utils.DisplayOP.Result (name, item, res, count) when res.Success ->
                    let std = Utils.GetStdEv(res.Record.Value, cutOff)
                    let total = (std * count)
                    sum <- sum + total
                    addList
                        name
                        (item.Name)
                        (std.ToString())
                        (String.Format("{0:0.###}", count))
                        (total.ToString())
                        res.UpdateDate
                | Utils.DisplayOP.Result (name, item, res, count) when not res.Success ->
                    addList
                        name
                        (item.Name)
                        "暂缺"
                        (String.Format("{0:0.###}", count))
                        "--"
                        "--"
                | Utils.DisplayOP.EndSum name -> 
                    addList
                        name
                        "总计"
                        "--"
                        "--"
                        (sum.ToString())
                        "--"
                | Utils.DisplayOP.Error str ->
                    addList str "" "" "" "" ""
                | _ -> failwithf ""
        list.Items.AddRange(buf.ToArray())
        tp.Invoke(new Action(fun () -> tp.Controls.Add(list))) |> ignore