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
            x.Test(title, queries)

    member private x.Test (title, queries) = 
        let list = new ListView(
                        View = View.Details,
                        FullRowSelect = true,
                        GridLines     = true,
                        AllowColumnReorder = true,
                        MultiSelect   = true,
                        Dock          = DockStyle.Fill)

        list.Columns.Add("Query").Width <- -2
        list.Columns.Add("Item").Width <- -2
        list.Columns.Add("Price").Width <- -2
        list.Columns.Add("Count").Width <- -2
        list.Columns.Add("Total").Width <- -2
        list.Columns.Add("Last Update").Width <- -2

        let AddFetchResult (query : Utils.Query, res : LibXIVDMF.Market.MarketFetchResult, count : int) = 
            let cutOff = 25
            let arr = 
                [|
                    if res.Success then
                        let std =  Utils.GetStdEv(res.Records.Value, cutOff)
                        yield query.ToString()
                        yield res.Item.GetName()
                        yield std.ToString()
                        yield count.ToString()
                        yield std.Plus(count |> float).ToString()
                        yield res.Updated
                    else
                        yield query.ToString()
                        yield res.Item.GetName()
                        yield "暂缺"
                        yield count.ToString()
                        yield "--"
                        yield "--"
                |]
            list.Items.Add(new ListViewItem(arr)) |> ignore

        for q in queries do 
            let item = q.TryGetItem()
            if item.IsSome then
                let item = item.Value
                match q with 
                | Utils.Query.Item         x ->
                    let x = LibXIVDMF.Market.FetchMarketData(item)
                    AddFetchResult(q, x, 1)
                | Utils.Query.Materials    x ->
                    let recipe = LibFFXIV.Database.SuRecipeData.Instance.GetMaterials(item.LodestoneId)
                    if recipe.IsSome then
                        recipe.Value
                        |> Array.map (fun (item, count) -> LibXIVDMF.Market.FetchMarketData(item), count)
                        |> Array.iter(fun (result, count) -> AddFetchResult(q, result, count))
                    else
                        list.Items.Add(new ListViewItem("找不到配方"+q.TryGetItem().ToString())) |> ignore
                | Utils.Query.MaterialsRec x ->
                    failwithf "尚未实现递归配方"
            else
                list.Items.Add(new ListViewItem("找不到物品"+q.ToString())) |> ignore

        let tp = new TabPage(title)
        tp.Controls.Add(list)
        tabControl1.TabPages.Add(tp)
        tabControl1.SelectedTab <- tp