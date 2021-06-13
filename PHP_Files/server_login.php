<?php
    $id = $_GET["id"];
    $pass = $_GET["pass"];

    if (!filter_var($id, FILTER_VALIDATE_INT)) {
        echo "ID '$id' is considered invalid.\n";
        }
    elseif(preg_match("/\\s/", $pass)){
        echo "Password is considered invalid";
        }
    else{
        include"Connect.php";
        $query = "SELECT password FROM `KDev_server` WHERE id= $id LIMIT 1";
        if (!($result = $mysqli->query($query))){
            echo "ERROR: Connection faild";
        }
        else{
            $row = $result->fetch_assoc();
            if($pass == $row["password"]){
                session_start();
                $_SESSION['sessionID'] = $_GET["id"];
                echo $_SESSION['sessionID'];
            }
            else{
                echo "Incorrect E-mail/Password<br>", session_id();
            }
        }
    }
            
        
        
    
       
?>
