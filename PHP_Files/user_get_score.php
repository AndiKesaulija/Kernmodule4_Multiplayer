<?php
    session_start();

    $userID = $_GET["userID"];
    $serverID = $_GET["serverID"];
        
    //Connect
    include"Connect.php";

    $query = "SELECT * FROM `KDev_score` WHERE serverID= '$serverID' And userID = '$userID'";
    $scoreArray = array();

    if (!($result = $mysqli->query($query))){
        
        showerror($mysqli->errno, $mysqli->error);
        
    }else{

        $row = $result->fetch_assoc();
        do{
            array_push($scoreArray, $row);
        }while($row = $result->fetch_assoc());

        echo json_encode($scoreArray);
    }
?>

