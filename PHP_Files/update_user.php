<?php
    session_start();
    include "Connect.php";

    $user = $_GET['user'];
    $email = $_GET['email'];
    $password = $_GET['pass'];

    //0 = Correct. 1 = Invalid. 2 = In use.
    $insertArray = array("user" => 0,"email" => 0,"password" => 0);
   

    if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
        $insertArray['email'] = 1;
    }
    elseif(preg_match("/\\s/", $password)){
        $insertArray['password'] = 1;
    }


    
    if(isset($_GET['id'])){
        $id = $_GET['id'];
        $query = "SELECT * FROM `KDev_users` WHERE id= '$id'"; 
        
        if(!$result = $mysqli->query($query)){
            showerror($mysqli->errno, $mysqli->error);
        }
        else{
            $row = $result->fetch_assoc();
            
            //Update existing User ID - User - Email - Password
            $dbID = $row["id"];
            $dbName = $user;
            $dbEmail = $email;
            $dbPassword = $password;

            
            $update = "UPDATE `KDev_users` SET name='$dbName', email='$dbEmail', password='$dbPassword' WHERE id= '$dbID'";
            
            if(!$mysqli->query($update)){
                showerror($mysqli->errno, $mysqli->error);
                
            }else{
                echo $dbID , $dbName, $dbEmail, $dbPassword;
            }
        }
    }else{//Insert New User
        
        //Check if name and email are in use
        $nameQuery = "SELECT name, email FROM `KDev_users`";
        if(!$nameResult = $mysqli->query($nameQuery)){
            showerror($mysqli->errno, $mysqli->error);
        }else{
            $row = $nameResult->fetch_assoc();
            do{
                if($row['name'] == $user){
                    $insertArray['user'] = 2;
                }
                if($row['email'] == $email){
                    $insertArray['email'] = 2;
                }
            }while($row = $nameResult->fetch_assoc());
        }
        
        if($insertArray['user'] != 0 ||$insertArray['email'] != 0 || $insertArray['password'] != 0 ){
            echo json_encode($insertArray);
        }else{
            //New qurey All usernames,passwords,email
            $query = "INSERT INTO `KDev_users` (`id`, `name`, `email`, `password`) VALUES (NULL, '$user', '$email', '$password')";
            if ($mysqli->query($query) === TRUE) 
            {
                echo "New User created";
            } 
        }
    }
    
    

?>
