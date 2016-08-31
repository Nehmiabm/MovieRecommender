
$(document).ready(function () {

    //Recommend movies
    $("#recommendMe").click(function () {
        var userid = $("#userId").val();
        var table = $("#recomMovies").DataTable({
            "bDestroy": true,
            ajax: {
                
                url: "/api/Movies/recommendations/" + userid,
                dataSrc: "",
                error: function (jqXHR, textStatus, errorThrown) {
                    bootbox.alert("Error calculating recommendations:" + jqXHR.responseText);

                }
            },
            columns: [
                {
                    data: "title",
                    render: function (data, type, recommend) {
                        return "<a target='_blank' href='http://www.imdb.com/title/tt" + recommend.movieId + "'>" + recommend.title + "</a>";
                    }
                },

                {
                    data: "genre",
                    render: function (data) {
                        return data;
                    }
                },
                 {
                     data: "year",
                     render: function (data) {
                         return data;
                     }
                 }
            ]
        });
    })

    //Search movies
    $("#search")
        .click(function() {
            var title = $("#title").val();
            var table = $("#moviesTable")
                .DataTable({
                    "bDestroy": true,
                    ajax: {
                        url: "/api/Movies/" + title,
                        dataSrc: "",
                        error: function (jqXHR, textStatus, errorThrown) {
                            bootbox.alert("Error searching movies:" + jqXHR.responseText);

                        }
                    },
                    columns: [
                        {
                            data: "title",
                            render: function(data) {
                                return data;
                            }
                        },
                        {
                            data: "genre",
                            render: function(data) {
                                return data;
                            }
                        },
                        {
                            data: "year",
                            render: function(data) {
                                return data;
                            }
                        },
                        {
                            data: "movieId",
                            render: function(data) {
                                return "<button class='btn-link js-rate' data-movie-id=" +
                                    data +
                                    "><span class='glyphicon glyphicon-star'><p>Rate</p></span></button>";
                            }
                        }
                    ]
                });
        });

    //Show Rating Dialog
    $("#moviesTable")
        .on("click",
            ".js-rate",
            function () {
                $("#ratingModal").modal("show");
                var button = $(this);
                var mid = button.attr("data-movie-id");
               
                $(".my-rating").starRating({
                    starSize: 25,
                    disableAfterRate:false,
                    totalStars: 10,
                    initialRating:0,
                    callback: function (currentRating, $el) {
                        var ratinguid = $("#rateuserId").val();
                        var ratingJson = {
                            userId: ratinguid,
                            movieId: mid,
                            preference: currentRating
                        };
                        // make a server call here
                        $.ajax({
                            url: "/api/Movies/Rating",
                            type: 'post',
                            datatype: 'json',
                            success: function() {
                                bootbox.alert("Rate posted successfully!");
                                $el.starRating('setRating', 0);
                                $("#ratingModal").modal("hide");
                            },
                            error: function (jqXHR, textStatus, errorThrown) {
                                bootbox.alert("Error saving movie rate:" + jqXHR.responseText);
                                $el.starRating('setRating', 0);
                                $("#ratingModal").modal("hide");
                                
                            },
                            data: ratingJson
                        });
                    }
                });

            });

});
