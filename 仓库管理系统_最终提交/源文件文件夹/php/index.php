<?php
require_once "config.php";

$keyword = isset($_GET["keyword"]) ? trim($_GET["keyword"]) : "";
if ($keyword !== "") {
    $safeKeyword = mysqli_real_escape_string($conn, $keyword);
    $sql = "SELECT * FROM parts
            WHERE part_no LIKE '%$safeKeyword%'
               OR part_name LIKE '%$safeKeyword%'
               OR part_color LIKE '%$safeKeyword%'
            ORDER BY purchase_time DESC, part_no ASC";
} else {
    $sql = "SELECT * FROM parts ORDER BY purchase_time DESC, part_no ASC";
}
$result = mysqli_query($conn, $sql);
?>
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>仓库零件管理系统</title>
    <link rel="stylesheet" href="style.css">
</head>
<body>
<div class="wrap">
    <h1>仓库零件管理系统</h1>
    <div class="toolbar">
        <form method="get" action="index.php">
            <input type="text" name="keyword" value="<?php echo htmlspecialchars($keyword); ?>" placeholder="按零件号、名称、颜色查询">
            <button type="submit">查询</button>
            <a class="btn" href="index.php">全部</a>
        </form>
        <a class="btn primary" href="part_add.php">添加零件</a>
    </div>
    <table>
        <tr>
            <th>零件号</th>
            <th>零件名</th>
            <th>颜色</th>
            <th>数量</th>
            <th>购买时间</th>
            <th>备注</th>
            <th>操作</th>
        </tr>
        <?php while ($row = mysqli_fetch_assoc($result)) { ?>
        <tr>
            <td><?php echo htmlspecialchars($row["part_no"]); ?></td>
            <td><?php echo htmlspecialchars($row["part_name"]); ?></td>
            <td><?php echo htmlspecialchars($row["part_color"]); ?></td>
            <td><?php echo htmlspecialchars($row["quantity"]); ?></td>
            <td><?php echo htmlspecialchars($row["purchase_time"]); ?></td>
            <td><?php echo htmlspecialchars($row["remark"]); ?></td>
            <td>
                <a href="part_view.php?part_no=<?php echo urlencode($row["part_no"]); ?>">查看</a>
                <a href="part_edit.php?part_no=<?php echo urlencode($row["part_no"]); ?>">修改</a>
                <a href="part_delete.php?part_no=<?php echo urlencode($row["part_no"]); ?>" onclick="return confirm('确定删除该零件信息吗？');">删除</a>
            </td>
        </tr>
        <?php } ?>
    </table>
</div>
</body>
</html>
