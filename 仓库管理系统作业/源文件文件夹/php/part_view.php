<?php
require_once "config.php";

$partNo = isset($_GET["part_no"]) ? mysqli_real_escape_string($conn, $_GET["part_no"]) : "";
$sql = "SELECT * FROM parts WHERE part_no='$partNo'";
$result = mysqli_query($conn, $sql);
$row = mysqli_fetch_assoc($result);
if (!$row) {
    die("未找到该零件信息。");
}
?>
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>查看零件</title>
    <link rel="stylesheet" href="style.css">
</head>
<body>
<div class="wrap narrow">
    <h1>查看零件信息</h1>
    <dl class="detail">
        <dt>零件号</dt><dd><?php echo htmlspecialchars($row["part_no"]); ?></dd>
        <dt>零件名</dt><dd><?php echo htmlspecialchars($row["part_name"]); ?></dd>
        <dt>零件颜色</dt><dd><?php echo htmlspecialchars($row["part_color"]); ?></dd>
        <dt>零件数量</dt><dd><?php echo htmlspecialchars($row["quantity"]); ?></dd>
        <dt>购买时间</dt><dd><?php echo htmlspecialchars($row["purchase_time"]); ?></dd>
        <dt>备注</dt><dd><?php echo htmlspecialchars($row["remark"]); ?></dd>
    </dl>
    <a class="btn primary" href="part_edit.php?part_no=<?php echo urlencode($row["part_no"]); ?>">修改</a>
    <a class="btn" href="index.php">返回</a>
</div>
</body>
</html>
