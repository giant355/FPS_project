<?php
header("Content-Type: text/html; charset=utf-8");

$host = "localhost";
$user = "root";
$password = "";
$database = "warehouse_db";

$conn = mysqli_connect($host, $user, $password, $database);
if (!$conn) {
    die("数据库连接失败：" . mysqli_connect_error());
}

mysqli_query($conn, "SET NAMES utf8");
?>
