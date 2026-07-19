SET NAMES utf8;
SET FOREIGN_KEY_CHECKS = 0;

DROP DATABASE IF EXISTS warehouse_db;
CREATE DATABASE warehouse_db DEFAULT CHARACTER SET utf8 COLLATE utf8_general_ci;
USE warehouse_db;

DROP TABLE IF EXISTS parts;
CREATE TABLE parts (
  part_no VARCHAR(20) NOT NULL COMMENT '零件号',
  part_name VARCHAR(50) NOT NULL COMMENT '零件名',
  part_color VARCHAR(20) NOT NULL COMMENT '零件颜色',
  quantity INT NOT NULL DEFAULT 0 COMMENT '零件数量',
  purchase_time DATE NOT NULL COMMENT '购买时间',
  remark VARCHAR(200) DEFAULT NULL COMMENT '备注',
  created_at DATETIME NOT NULL COMMENT '创建时间',
  updated_at DATETIME NOT NULL COMMENT '修改时间',
  PRIMARY KEY (part_no)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='仓库零件信息表';

INSERT INTO parts (part_no, part_name, part_color, quantity, purchase_time, remark, created_at, updated_at) VALUES
('P001', '螺丝', '银色', 500, '2026-03-12', '常用标准件', NOW(), NOW()),
('P002', '轴承', '黑色', 120, '2026-03-20', '设备维修备用', NOW(), NOW()),
('P003', '垫片', '银色', 300, '2026-04-02', '配套零件', NOW(), NOW()),
('P004', '齿轮', '灰色', 80, '2026-04-18', '机械传动件', NOW(), NOW()),
('P005', '弹簧', '黑色', 150, '2026-05-06', '弹性组件', NOW(), NOW());

SET FOREIGN_KEY_CHECKS = 1;
