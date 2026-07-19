<?php
require_once "config.php";

$message = "";
if ($_SERVER["REQUEST_METHOD"] == "POST") {
    $partNo = trim($_POST["part_no"]);
    $partName = trim($_POST["part_name"]);
    $partColor = trim($_POST["part_color"]);
    $quantity = intval($_POST["quantity"]);
    $purchaseTime = trim($_POST["purchase_time"]);
    $remark = trim($_POST["remark"]);

    if ($partNo == "" || $partName == "" || $partColor == "" || $purchaseTime == "") {
        $message = "请填写完整的零件信息。";
    } elseif ($quantity < 0) {
        $message = "零件数量不能小于 0。";
    } else {
        $partNo = mysqli_real_escape_string($conn, $partNo);
        $partName = mysqli_real_escape_string($conn, $partName);
        $partColor = mysqli_real_escape_string($conn, $partColor);
        $purchaseTime = mysqli_real_escape_string($conn, $purchaseTime);
        $remark = mysqli_real_escape_string($conn, $remark);

        $sql = "INSERT INTO parts(part_no, part_name, part_color, quantity, purchase_time, remark, created_at, updated_at)
                VALUES('$partNo', '$partName', '$partColor', $quantity, '$purchaseTime', '$remark', NOW(), NOW())";
        if (mysqli_query($conn, $sql)) {
            header("Location: index.php");
            exit;
        } else {
            $message = "添加失败：" . mysqli_error($conn);
        }
    }
}
?>
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>添加零件</title>
    <link rel="stylesheet" href="style.css">
</head>
<body>
<div class="wrap narrow">
    <h1>添加零件信息</h1>
    <?php if ($message != "") { ?><p class="error"><?php echo htmlspecialchars($message); ?></p><?php } ?>
    <form method="post">
        <label>零件号<input type="text" name="part_no" required></label>
        <label>零件名<input type="text" name="part_name" required></label>
        <label>零件颜色<input type="text" name="part_color" required></label>
        <label>零件数量<input type="number" name="quantity" min="0" value="0" required></label>
        <label>购买时间<input type="date" name="purchase_time" required></label>
        <label>备注<textarea name="remark"></textarea></label>
        <button type="submit">保存</button>
        <a class="btn" href="index.php">返回</a>
    </form>
</div>
</body>
</html>
