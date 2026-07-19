<?php
require_once "config.php";

if (isset($_GET["part_no"])) {
    $partNo = mysqli_real_escape_string($conn, $_GET["part_no"]);
    $sql = "DELETE FROM parts WHERE part_no='$partNo'";
    mysqli_query($conn, $sql);
}

header("Location: index.php");
exit;
?>
