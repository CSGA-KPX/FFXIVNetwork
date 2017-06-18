module UI
open System
open System.Windows.Forms

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
            let line = textBox1.Text
            let (title, queries) = 
                let sp = line.Split(':', '：')
                if sp.Length <= 1 then
                    ("None", line.Split(',', '，') |> Array.map (Utils.Query.FromString))
                else
                    (sp.[0], sp.[1].Split(',', '，') |> Array.map (Utils.Query.FromString))
            x.TestAsync(title, queries)

    member private x.TestAsync (title, queries) = 
        let tp = new TabPage(title)
        
        Threading.Tasks.Task.Run(new Action(fun () -> 
            x.Test(title, queries, tp)
              )) |> ignore

        tabControl1.TabPages.Add(tp)
        tabControl1.SelectedTab <- tp

    member private x.Test (title, queries, tp) = 
        let list = new ListView(
                        View = View.Details,
                        FullRowSelect = true,
                        GridLines     = true,
                        AllowColumnReorder = true,
                        MultiSelect   = true,
                        Dock          = DockStyle.Fill)
        [| "Query"; "Item"; "Price"; "Count"; "Total"; "Last Update" |]
        |> Array.iter (fun x -> list.Columns.Add(x).Width <- -2)

        let cutOff = 25

        let buf = Collections.Generic.List<ListViewItem>()
        for q in queries do 
            try 
                let ms = q.GetMaterials() |> Array.sortBy (fun (a, b) -> a.Item.XIVDbId)
                let mutable total = Utils.StdEv.Zero
                for (res, count) in ms do 
                    let arr = 
                        [|
                            if res.Success then
                                let std =  Utils.GetStdEv(res.Records.Value, cutOff)
                                total <- total + (std * count)
                                yield q.ToString()
                                yield res.Item.GetName()
                                yield std.ToString()
                                yield String.Format("{0:0.###}", count)
                                yield (std * count).ToString()
                                yield res.Updated
                            else
                                yield q.ToString()
                                yield res.Item.GetName()
                                yield "暂缺"
                                yield String.Format("{0:0.###}", count)
                                yield "--"
                                yield "--"
                        |]
                    buf.Add(new ListViewItem(arr))
                let sumUp = [| q.ToString(); "总计"; "--"; "--"; total.ToString(); "--" |]
                buf.Add(new ListViewItem(sumUp))
            with 
            | Failure(msg) ->
                let arr = [| q.ToString(); msg; "--"; "--"; "--"; "--" |]
                buf.Add(new ListViewItem(arr))
        list.Items.AddRange(buf.ToArray())
        tp.Invoke(new Action(fun () -> tp.Controls.Add(list))) |> ignore