<?php
require_once "config.php";

$partNo = isset($_GET["part_no"]) ? mysqli_real_escape_string($conn, $_GET["part_no"]) : "";
$message = "";

if ($_SERVER["REQUEST_METHOD"] == "POST") {
    $oldPartNo = mysqli_real_escape_string($conn, $_POST["old_part_no"]);
    $partName = mysqli_real_escape_string($conn, trim($_POST["part_name"]));
    $partColor = mysqli_real_escape_string($conn, trim($_POST["part_color"]));
    $quantity = intval($_POST["quantity"]);
    $purchaseTime = mysqli_real_escape_string($conn, trim($_POST["purchase_time"]));
    $remark = mysqli_real_escape_string($conn, trim($_POST["remark"]));

    if ($partName == "" || $partColor == "" || $purchaseTime == "") {
        $message = "请填写完整的零件信息。";
    } elseif ($quantity < 0) {
        $message = "零件数量不能小于 0。";
    } else {
        $sql = "UPDATE parts SET
                    part_name='$partName',
                    part_color='$partColor',
                    quantity=$quantity,
                    purchase_time='$purchaseTime',
                    remark='$remark',
                    updated_at=NOW()
                WHERE part_no='$oldPartNo'";
        if (mysqli_query($conn, $sql)) {
            header("Location: index.php");
            exit;
        } else {
            $message = "修改失败：" . mysqli_error($conn);
        }
    }
    $partNo = $oldPartNo;
}

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
    <title>修改零件</title>
    <link rel="stylesheet" href="style.css">
</head>
<body>
<div class="wrap narrow">
    <h1>修改零件信息</h1>
    <?php if ($message != "") { ?><p class="error"><?php echo htmlspecialchars($message); ?></p><?php } ?>
    <form method="post">
        <input type="hidden" name="old_part_no" value="<?php echo htmlspecialchars($row["part_no"]); ?>">
        <label>零件号<input type="text" value="<?php echo htmlspecialchars($row["part_no"]); ?>" disabled></label>
        <label>零件名<input type="text" name="part_name" value="<?php echo htmlspecialchars($row["part_name"]); ?>" required></label>
        <label>零件颜色<input type="text" name="part_color" value="<?php echo htmlspecialchars($row["part_color"]); ?>" required></label>
        <label>零件数量<input type="number" name="quantity" min="0" value="<?php echo htmlspecialchars($row["quantity"]); ?>" required></label>
        <label>购买时间<input type="date" name="purchase_time" value="<?php echo htmlspecialchars($row["purchase_time"]); ?>" required></label>
        <label>备注<textarea name="remark"><?php echo htmlspecialchars($row["remark"]); ?></textarea></label>
        <button type="submit">保存修改</button>
        <a class="btn" href="index.php">返回</a>
    </form>
</div>
</body>
</html>
